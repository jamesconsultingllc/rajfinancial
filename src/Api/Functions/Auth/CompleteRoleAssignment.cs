using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using RajFinancial.Api.Services;
using RajFinancial.Shared.Contracts.Auth;

namespace RajFinancial.Api.Functions.Auth;

/// <summary>
/// Completes role assignment after user creation.
/// This is called by the CompleteProfile.razor page after first login
/// as a fallback if the API Connector approach doesn't work.
/// </summary>
public class CompleteRoleAssignment
{
    private readonly ILogger<CompleteRoleAssignment> _logger;
    private readonly IGraphClientWrapper _graphClient;
    private readonly IConfiguration _configuration;
    private readonly IValidator<CompleteRoleRequest> _validator;

    // Map role names to their GUIDs from app registration
    // TODO: Update these GUIDs to match your actual app roles
    private static readonly Dictionary<string, Guid> RoleMapping = new()
    {
        { "Client", Guid.Parse("00000000-0000-0000-0000-000000000003") },
        { "Advisor", Guid.Parse("00000000-0000-0000-0000-000000000002") },
        { "Administrator", Guid.Parse("00000000-0000-0000-0000-000000000001") }
    };

    public CompleteRoleAssignment(
        ILogger<CompleteRoleAssignment> logger,
        IGraphClientWrapper graphClient,
        IConfiguration configuration,
        IValidator<CompleteRoleRequest> validator)
    {
        _logger = logger;
        _graphClient = graphClient;
        _configuration = configuration;
        _validator = validator;
    }

    /// <summary>
    /// Assigns an app role to a user via Microsoft Graph API.
    /// </summary>
    /// <param name="req">HTTP request with userId and role.</param>
    /// <returns>Success or error response.</returns>
    [Function("CompleteRoleAssignment")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "auth/complete-role")]
        HttpRequestData req)
    {
        try
        {
            // 1. Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CompleteRoleRequest? data;
            
            try
            {
                data = JsonSerializer.Deserialize<CompleteRoleRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON in request body");
                var jsonErrorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await jsonErrorResponse.WriteAsJsonAsync(new CompleteRoleErrorResponse
                {
                    Code = "VALIDATION_FAILED",
                    Error = "UserId and Role are required"
                });
                return jsonErrorResponse;
            }

            // 2. Validate request using FluentValidation
            if (data == null)
            {
                _logger.LogWarning("Invalid request - null request body");
                var nullErrorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await nullErrorResponse.WriteAsJsonAsync(new CompleteRoleErrorResponse
                {
                    Code = "VALIDATION_FAILED",
                    Error = "UserId and Role are required"
                });
                return nullErrorResponse;
            }

            var validationResult = await _validator.ValidateAsync(data);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();
                _logger.LogWarning("Validation failed: {Error}", firstError.ErrorMessage);
                
                var validationErrorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationErrorResponse.WriteAsJsonAsync(new CompleteRoleErrorResponse
                {
                    Code = firstError.ErrorCode,
                    Error = firstError.ErrorMessage
                });
                return validationErrorResponse;
            }

            // 3. Get service principal ID from configuration
            var servicePrincipalId = _configuration["EntraExternalId:ServicePrincipalId"];
            if (string.IsNullOrEmpty(servicePrincipalId))
            {
                _logger.LogError("ServicePrincipalId not configured");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new CompleteRoleErrorResponse
                {
                    Code = "CONFIGURATION_ERROR",
                    Error = "Service principal configuration missing"
                });
                return errorResponse;
            }

            // 4. Check if user already has this role assigned
            var existingAssignments = await _graphClient
                .GetUserAppRoleAssignmentsAsync(data.UserId);

            var hasRole = existingAssignments?.Value?.Any(a =>
                a.AppRoleId == RoleMapping[data.Role] &&
                a.ResourceId.ToString() == servicePrincipalId) ?? false;

            if (hasRole)
            {
                _logger.LogInformation(
                    "User {UserId} already has role {Role} assigned",
                    data.UserId, data.Role);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new CompleteRoleResponse
                {
                    Success = true,
                    Role = data.Role,
                    Message = "Role already assigned"
                });
                return response;
            }

            // 5. Assign app role via Microsoft Graph
            var appRoleAssignment = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(data.UserId),
                ResourceId = Guid.Parse(servicePrincipalId),
                AppRoleId = RoleMapping[data.Role]
            };

            await _graphClient.AssignAppRoleToUserAsync(data.UserId, appRoleAssignment);

            _logger.LogInformation(
                "Successfully assigned role {Role} to user {UserId}",
                data.Role, data.UserId);

            // 6. Return success response
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            await successResponse.WriteAsJsonAsync(new CompleteRoleResponse
            {
                Success = true,
                Role = data.Role
            });

            return successResponse;
        }
        catch (ServiceException ex) when (ex.Message.Contains("Permission grants"))
        {
            _logger.LogError(ex, "Insufficient permissions to assign app roles");
            var errorResponse = req.CreateResponse(HttpStatusCode.Forbidden);
            await errorResponse.WriteAsJsonAsync(new CompleteRoleErrorResponse
            {
                Code = "INSUFFICIENT_PERMISSIONS",
                Error = "Insufficient permissions. Ensure Managed Identity has AppRoleAssignment.ReadWrite.All permission."
            });
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing role assignment");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new CompleteRoleErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Error = "Internal server error"
            });
            return errorResponse;
        }
    }
}
