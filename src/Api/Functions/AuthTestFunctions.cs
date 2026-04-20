using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Observability;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Test endpoints for validating authentication and role-based access control.
///     These endpoints are useful for verifying Entra External ID integration.
/// </summary>
/// <remarks>
///     <para>
///         <b>Endpoints:</b>
///         <list type="bullet">
///             <item><c>GET /api/auth/status</c> - Returns current user info (requires authentication)</item>
///             <item><c>GET /api/auth/client</c> - Accessible by any authenticated user (implicit Client role)</item>
///             <item><c>GET /api/auth/admin</c> - Requires explicit Administrator role</item>
///             <item><c>GET /api/auth/public</c> - No authentication required</item>
///         </list>
///     </para>
/// </remarks>
public partial class AuthTestFunctions(
    ILogger<AuthTestFunctions> logger,
    IOptions<AppRoleOptions> appRoleOptions)
{
    private readonly AppRoleOptions appRoles = appRoleOptions.Value;

    /// <summary>
    ///     Returns basic authentication status for the currently authenticated user.
    ///     Requires authentication but no specific role. Used for integration test
    ///     validation of the auth middleware pipeline.
    /// </summary>
    /// <remarks>
    ///     The production user profile endpoint is <c>GET /api/auth/me</c>
    ///     (see <see cref="AuthFunctions.GetMe"/>).
    /// </remarks>
    [RequireAuthentication]
    [Function("AuthStatus")]
    public async Task<HttpResponseData> GetAuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/status")]
        HttpRequestData req,
        FunctionContext context)
    {
        using var activity = AuthTelemetry.StartActivity("Auth.Status");
        activity?.SetTag("http.method", req.Method);
        activity?.SetTag("http.route", "auth/status");

        var userId = context.GetUserId();
        var email = context.GetUserEmail();
        var name = context.GetUserName();
        var roles = context.GetUserRoles();

        activity?.SetTag("user.id", userId);
        AuthTelemetry.RecordSuccess(new KeyValuePair<string, object?>("endpoint", "auth/status"));
        LogAuthStatusAccessed(userId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add(HttpHeaderNames.ContentType, FunctionHelpers.JsonContentType);

        var payload = new
        {
            authenticated = true,
            userId,
            email,
            name,
            roles,
            isAdministrator = context.HasRole("Administrator"),
            timestamp = DateTime.UtcNow
        };
        await response.WriteStringAsync(JsonSerializer.Serialize(payload));

        return response;
    }

    /// <summary>
    ///     Endpoint accessible by any authenticated user.
    ///     Demonstrates implicit Client role - all authenticated users are Clients.
    /// </summary>
    [RequireAuthentication]
    [Function("AuthClient")]
    public async Task<HttpResponseData> GetClientData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/client")]
        HttpRequestData req,
        FunctionContext context)
    {
        using var activity = AuthTelemetry.StartActivity("Auth.Client");
        activity?.SetTag("http.method", req.Method);
        activity?.SetTag("http.route", "auth/client");

        var userId = context.GetUserId();
        activity?.SetTag("user.id", userId);
        AuthTelemetry.RecordSuccess(new KeyValuePair<string, object?>("endpoint", "auth/client"));
        LogClientEndpointAccessed(userId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add(HttpHeaderNames.ContentType, FunctionHelpers.JsonContentType);

        var payload = new
        {
            message = "Welcome, Client!",
            access = "client",
            userId,
            description = "This endpoint is accessible by any authenticated user (implicit Client role)",
            timestamp = DateTime.UtcNow
        };
        await response.WriteStringAsync(JsonSerializer.Serialize(payload));

        return response;
    }

    /// <summary>
    ///     Endpoint restricted to users with explicit Administrator role.
    ///     Demonstrates role-based access control.
    /// </summary>
    [RequireRole("Administrator")]
    [Function("AuthAdmin")]
    public async Task<HttpResponseData> GetAdminData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/admin")]
        HttpRequestData req,
        FunctionContext context)
    {
        using var activity = AuthTelemetry.StartActivity("Auth.Admin");
        activity?.SetTag("http.method", req.Method);
        activity?.SetTag("http.route", "auth/admin");

        var adminUserId = context.GetUserId();
        activity?.SetTag("user.id", adminUserId);
        AuthTelemetry.RecordSuccess(new KeyValuePair<string, object?>("endpoint", "auth/admin"));
        LogAdminEndpointAccessed(adminUserId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add(HttpHeaderNames.ContentType, FunctionHelpers.JsonContentType);

        var payload = new
        {
            message = "Welcome, Administrator!",
            access = "administrator",
            userId = adminUserId,
            description = "This endpoint requires explicit Administrator role assignment",
            adminRoleId = appRoles.Administrator,
            timestamp = DateTime.UtcNow
        };
        await response.WriteStringAsync(JsonSerializer.Serialize(payload));

        return response;
    }

    /// <summary>
    ///     Public endpoint that does not require authentication.
    ///     Useful for verifying the API is reachable.
    /// </summary>
    [Function("AuthPublic")]
    public async Task<HttpResponseData> GetPublicData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/public")]
        HttpRequestData req,
        FunctionContext context)
    {
        using var activity = AuthTelemetry.StartActivity("Auth.Public");
        activity?.SetTag("http.method", req.Method);
        activity?.SetTag("http.route", "auth/public");

        var isAuthenticated = context.IsAuthenticated();
        var userId = isAuthenticated ? context.GetUserId() : null;
        activity?.SetTag("auth.authenticated", isAuthenticated);

        LogPublicEndpointAccessed(isAuthenticated);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add(HttpHeaderNames.ContentType, FunctionHelpers.JsonContentType);

        var payload = new
        {
            message = "This is a public endpoint",
            access = "public",
            authenticated = isAuthenticated,
            userId,
            description = "No authentication required to access this endpoint",
            timestamp = DateTime.UtcNow
        };
        await response.WriteStringAsync(JsonSerializer.Serialize(payload));

        return response;
    }

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "User {UserId} accessed /api/auth/status")]
    private partial void LogAuthStatusAccessed(string? userId);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Client {UserId} accessed /api/auth/client")]
    private partial void LogClientEndpointAccessed(string? userId);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Information, Message = "Administrator {UserId} accessed /api/auth/admin")]
    private partial void LogAdminEndpointAccessed(string? userId);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Information, Message = "Public endpoint accessed. Authenticated: {IsAuthenticated}")]
    private partial void LogPublicEndpointAccessed(bool isAuthenticated);
}
