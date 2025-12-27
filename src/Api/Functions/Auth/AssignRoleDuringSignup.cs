using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using RajFinancial.Api.Services;

namespace RajFinancial.Api.Functions.Auth;

/// <summary>
/// API Connector endpoint called by Entra External ID during user sign-up.
/// Automatically assigns app roles based on the signup button clicked.
/// </summary>
/// <remarks>
/// This function is called as an API Connector at the "Before creating user" step
/// in the Entra External ID user flow. It stores the requested role in a custom
/// attribute, which is then used by CompleteRoleAssignment after the user is created.
/// </remarks>
public class AssignRoleDuringSignup
{
    private readonly ILogger<AssignRoleDuringSignup> _logger;
    private readonly IGraphClientWrapper _graphClient;
    private readonly IConfiguration _configuration;

    public AssignRoleDuringSignup(
        ILogger<AssignRoleDuringSignup> logger,
        IGraphClientWrapper graphClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _graphClient = graphClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Validates the requested role during sign-up and stores it for later assignment.
    /// </summary>
    /// <param name="req">HTTP request from Entra External ID API Connector.</param>
    /// <returns>API Connector response (Continue or ShowBlockPage).</returns>
    [Function("AssignRoleDuringSignup")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "auth/assign-role-signup")]
        HttpRequestData req)
    {
        try
        {
            if (req.Body == null || req.Body.Length == 0)
            {
                _logger.LogWarning("Empty request body in API Connector call");
                return CreateBlockPageResponse(req, "Invalid request. Please try again.");
            }

            // 1. Parse API Connector request body
            var data = await JsonSerializer.DeserializeAsync<ApiConnectorRequest>(req.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null || string.IsNullOrEmpty(data.Email))
            {
                _logger.LogWarning("Invalid API Connector request - missing email");
                return CreateBlockPageResponse(req, "Invalid request. Please try again.");
            }

            // 2. Extract requested role from ui_locales parameter
            // The signup button passes the role via ui_locales query parameter
            var requestedRole = data.UiLocales ?? "Client"; // Default to Client

            // 3. Validate role is allowed for self-signup
            if (!IsAllowedSelfSignupRole(requestedRole))
            {
                _logger.LogWarning(
                    "Invalid role requested during signup: {Role} by {Email}",
                    requestedRole, data.Email);
                requestedRole = "Client"; // Default to Client for security
            }

            _logger.LogInformation(
                "User signing up: {Email} with role: {Role}",
                data.Email, requestedRole);

            // 4. Return Continue with custom attribute to store requested role
            // This will be retrieved after user creation by CompleteRoleAssignment
            var response = req.CreateResponse(HttpStatusCode.OK);

            var responseData = new ApiConnectorResponse
            {
                Version = "1.0.0",
                Action = "Continue",
                Extension_RequestedRole = requestedRole
            };

            await response.WriteAsJsonAsync(responseData);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AssignRoleDuringSignup");

            // Don't block user signup if role validation fails
            // CompleteProfile page can handle it as fallback
            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new ApiConnectorResponse
            {
                Version = "1.0.0",
                Action = "Continue"
            });

            return response;
        }
    }

    /// <summary>
    /// Validates if the requested role is allowed for self-signup.
    /// </summary>
    /// <param name="role">The requested role.</param>
    /// <returns>True if the role is allowed, false otherwise.</returns>
    private static bool IsAllowedSelfSignupRole(string role)
    {
        // Only Client and Advisor can self-signup
        // Administrator must be manually assigned
        return role switch
        {
            "Client" => true,
            "Advisor" => true,
            "Administrator" => false, // Security: Cannot self-assign admin
            _ => false
        };
    }

    /// <summary>
    /// Creates a block page response to prevent signup.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="message">The error message to display.</param>
    /// <returns>HTTP response with block page action.</returns>
    private static HttpResponseData CreateBlockPageResponse(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        response.WriteString(JsonSerializer.Serialize(new ApiConnectorResponse
        {
            Version = "1.0.0",
            Action = "ShowBlockPage",
            UserMessage = message
        }));

        return response;
    }
}

/// <summary>
/// Request body from Entra External ID API Connector.
/// </summary>
public class ApiConnectorRequest
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? UiLocales { get; set; } // Used to pass role parameter
    public string? ObjectId { get; set; } // Only present after user creation
}

/// <summary>
/// Response to Entra External ID API Connector.
/// </summary>
public class ApiConnectorResponse
{
    public string Version { get; set; } = "1.0.0";
    public string Action { get; set; } = "Continue"; // "Continue" or "ShowBlockPage"
    public string? UserMessage { get; set; }
    public string? Extension_RequestedRole { get; set; } // Custom attribute
}
