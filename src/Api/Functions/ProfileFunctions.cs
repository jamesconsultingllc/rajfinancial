using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Services.UserProfiles;

namespace RajFinancial.Api.Functions;

/// <summary>
/// Exposes the persisted <see cref="Shared.Entities.UserProfile"/> for the
/// currently authenticated user.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>/api/auth/me</c> (which echoes JWT claims), this endpoint returns
/// the <b>database-backed</b> profile created by JIT provisioning. Integration
/// tests use it to verify that provisioning occurred correctly without needing
/// direct database access.
/// </para>
/// <para>
/// <b>Endpoints:</b>
/// <list type="bullet">
///   <item><c>GET /api/profile/me</c> – Returns the authenticated user's persisted profile</item>
/// </list>
/// </para>
/// </remarks>
public class ProfileFunctions(
    ILogger<ProfileFunctions> logger,
    IUserProfileService userProfileService)
{
    /// <summary>
    /// Shared JSON serializer options matching <see cref="Middleware.Content.SerializationFactory"/>
    /// conventions: camelCase naming, null-value omission.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Returns the persisted <see cref="Shared.Entities.UserProfile"/> for the
    /// currently authenticated user.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><b>200 OK</b> – JSON body with the user's profile fields.</item>
    ///   <item><b>401 Unauthorized</b> – No authenticated user context available.</item>
    ///   <item><b>404 Not Found</b> – Profile has not been provisioned yet
    ///     (body includes <c>PROFILE_NOT_FOUND</c> error code).</item>
    /// </list>
    /// </returns>
    [RequireAuthentication]
    [Function("ProfileMe")]
    public async Task<HttpResponseData> GetMyProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "profile/me")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            logger.LogWarning("ProfileMe called without UserIdGuid in context");
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            return unauthorizedResponse;
        }

        var profile = await userProfileService.GetByIdAsync(userIdGuid.Value);

        if (profile is null)
        {
            logger.LogWarning(
                "Profile not found for user {UserId}",
                userIdGuid.Value);

            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            notFoundResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await notFoundResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                code = "PROFILE_NOT_FOUND",
                message = "User profile has not been provisioned"
            }, JsonOptions));
            return notFoundResponse;
        }

        logger.LogInformation(
            "Returning profile for user {UserId}",
            userIdGuid.Value);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            id = profile.Id.ToString(),
            email = profile.Email,
            displayName = profile.DisplayName,
            role = profile.Role.ToString(),
            tenantId = profile.TenantId,
            isActive = profile.IsActive,
            isProfileComplete = profile.IsProfileComplete,
            createdAt = profile.CreatedAt,
            lastLoginAt = profile.LastLoginAt,
            updatedAt = profile.UpdatedAt
        }, JsonOptions));

        return response;
    }
}
