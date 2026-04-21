using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Services.UserProfile;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Functions;

/// <summary>
/// Exposes the persisted <see cref="UserProfile"/> for the
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
///   <item><c>PUT /api/profile/me</c> – Updates the authenticated user's editable profile fields</item>
/// </list>
/// </para>
/// </remarks>
public partial class ProfileFunctions(
    ILogger<ProfileFunctions> logger,
    IUserProfileService userProfileService,
    ISerializationFactory serializationFactory)
{

    /// <summary>
    /// Returns the persisted <see cref="UserProfile"/> for the
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
            LogProfileMeMissingContext();
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            return unauthorizedResponse;
        }

        Activity.Current?.SetTag(UserProfileTelemetry.TagUserId, userIdGuid.Value);

        var profile = await userProfileService.GetByIdAsync(userIdGuid.Value);

        if (profile is null)
        {
            LogProfileNotFound(userIdGuid.Value);

            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.NotFound,
                "PROFILE_NOT_FOUND", "User profile has not been provisioned");
        }

        LogProfileMeReturning(userIdGuid.Value);

        var prefs = ParsePreferences(profile.PreferencesJson);
        var responseDto = new UserProfileResponse
        {
            UserId = profile.Id.ToString(),
            DisplayName = profile.DisplayName,
            Locale = prefs.Locale,
            Timezone = prefs.Timezone,
            Currency = prefs.Currency,
            CreatedAt = profile.CreatedAt  // Implicit DateTimeOffset → DtoDateTime
        };

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, responseDto, serializationFactory);
    }

    // =========================================================================
    // PUT /api/profile/me
    // =========================================================================

    /// <summary>
    /// Updates the authenticated user's editable profile fields.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><b>200 OK</b> – Updated <see cref="UserProfileResponse"/>.</item>
    ///   <item><b>401 Unauthorized</b> – No authenticated user context available.</item>
    ///   <item><b>404 Not Found</b> – Profile has not been provisioned yet.</item>
    /// </list>
    /// </returns>
    [RequireAuthentication]
    [Function("UpdateProfileMe")]
    public async Task<HttpResponseData> UpdateMyProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "profile/me")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            LogUpdateProfileMissingContext();
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Unauthorized,
                MiddlewareErrorCodes.AuthRequired, "Authentication is required");
        }

        Activity.Current?.SetTag(UserProfileTelemetry.TagUserId, userIdGuid.Value);

        var request = await context.GetValidatedBodyAsync<UpdateProfileRequest>();

        var profile = await userProfileService.UpdateProfileAsync(
            userIdGuid.Value, request);

        if (profile is null)
        {
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.NotFound,
                "PROFILE_NOT_FOUND", "User profile has not been provisioned");
        }

        var updatedPrefs = ParsePreferences(profile.PreferencesJson);
        var responseDto = new UserProfileResponse
        {
            UserId = profile.Id.ToString(),
            DisplayName = profile.DisplayName,
            Locale = updatedPrefs.Locale,
            Timezone = updatedPrefs.Timezone,
            Currency = updatedPrefs.Currency,
            CreatedAt = profile.CreatedAt  // Implicit DateTimeOffset → DtoDateTime
        };

        LogUpdateProfileReturning(userIdGuid.Value);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, responseDto, serializationFactory);
    }

    /// <summary>
    /// Parses locale, timezone, and currency from the user's PreferencesJson.
    /// Returns defaults ("en-US", "America/New_York", "USD") if missing or malformed.
    /// </summary>
    private static (string Locale, string Timezone, string Currency) ParsePreferences(string? preferencesJson)
    {
        const string defaultLocale = "en-US";
        const string defaultTimezone = "America/New_York";
        const string defaultCurrency = "USD";

        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return (defaultLocale, defaultTimezone, defaultCurrency);
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            var root = doc.RootElement;

            var locale = root.TryGetProperty("locale", out var l) ? l.GetString() ?? defaultLocale : defaultLocale;
            var timezone = root.TryGetProperty("timezone", out var t) ? t.GetString() ?? defaultTimezone : defaultTimezone;
            var currency = root.TryGetProperty("currency", out var c) ? c.GetString() ?? defaultCurrency : defaultCurrency;

            return (locale, timezone, currency);
        }
        catch (JsonException)
        {
            return (defaultLocale, defaultTimezone, defaultCurrency);
        }
    }

    [LoggerMessage(EventId = 4100, Level = LogLevel.Warning, Message = "ProfileMe called without UserIdGuid in context")]
    private partial void LogProfileMeMissingContext();

    [LoggerMessage(EventId = 4101, Level = LogLevel.Warning, Message = "Profile not found for user {UserId}")]
    private partial void LogProfileNotFound(Guid userId);

    [LoggerMessage(EventId = 4102, Level = LogLevel.Information, Message = "Returning profile for user {UserId}")]
    private partial void LogProfileMeReturning(Guid userId);

    [LoggerMessage(EventId = 4103, Level = LogLevel.Warning, Message = "UpdateProfileMe called without UserIdGuid in context")]
    private partial void LogUpdateProfileMissingContext();

    [LoggerMessage(EventId = 4104, Level = LogLevel.Information, Message = "UpdateProfileMe returning updated profile for user {UserId}")]
    private partial void LogUpdateProfileReturning(Guid userId);
}
