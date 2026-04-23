using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Observability;
using RajFinancial.Api.Services.Auth;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that extracts and validates the authenticated user context from JWT claims.
/// </summary>
/// <remarks>
/// <para>
/// This middleware implements OWASP security requirements:
/// <list type="bullet">
///   <item>A01:2025 - Broken Access Control: Establishes user context for authorization</item>
///   <item>A07:2025 - Authentication Failures: Validates JWT claims</item>
/// </list>
/// </para>
/// <para>
/// The middleware resolves the <see cref="ClaimsPrincipal"/> by:
/// <list type="number">
///   <item>Using an explicit principal set in <c>Items[<see cref="FunctionContextKeys.ClaimsPrincipal"/>]</c>
///     by an upstream middleware or a unit test.</item>
///   <item>Parsing the <c>Authorization: Bearer ...</c> header and validating the token via the
///     configured <see cref="IJwtBearerValidator"/>.</item>
/// </list>
/// A missing or invalid token yields a non-authenticated request; downstream
/// <c>AuthorizationMiddleware</c> converts that into a 401 for endpoints marked
/// <c>[RequireAuthentication]</c>.
/// </para>
/// <para>
/// <b>Context Items Set:</b>
/// <list type="bullet">
///   <item><c>UserId</c> / <c>UserIdGuid</c> — Entra Object ID (<c>oid</c>)</item>
///   <item><c>UserEmail</c>, <c>UserName</c>, <c>UserRoles</c></item>
///   <item><c>TenantId</c> — Entra tenant GUID (<c>tid</c>), when parseable</item>
///   <item><c>ClaimsPrincipal</c>, <c>IsAuthenticated</c></item>
/// </list>
/// </para>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public partial class AuthenticationMiddleware(
    ILogger<AuthenticationMiddleware> logger,
    IJwtBearerValidator validator) : IFunctionsWorkerMiddleware
{
    // Standard claim types for Entra External ID
    private const string OBJECT_ID_CLAIM = JwtClaimNames.ObjectIdLongForm;
    private const string OBJECT_ID_CLAIM_ALT = JwtClaimNames.Oid;
    private const string EMAIL_CLAIM = JwtClaimNames.Emails;
    private const string EMAIL_CLAIM_ALT = JwtClaimNames.Email;
    private const string PREFERRED_USERNAME_CLAIM = JwtClaimNames.PreferredUsername;
    private const string UPN_CLAIM = JwtClaimNames.UpnLongForm;
    private const string ROLES_CLAIM = JwtClaimNames.Roles;
    private const string NAME_CLAIM = JwtClaimNames.Name;
    private const string TENANT_ID_CLAIM = JwtClaimNames.Tid;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var activity = AuthTelemetry.StartActivity(AuthTelemetry.ActivityAuthenticate);
        activity?.SetTag("code.function", context.FunctionDefinition.Name);
        activity?.SetTag("faas.invocation_id", context.InvocationId);

        var (principal, failureReason) = await GetClaimsPrincipalAsync(context);

        if (principal?.Identity?.IsAuthenticated == true && PopulateAuthenticatedContext(context, principal))
        {
            var userId = context.Items.TryGetValue(FunctionContextKeys.UserId, out var uid)
                ? uid as string
                : null;
            activity?.SetTag("auth.authenticated", true);
            if (!string.IsNullOrEmpty(userId))
                activity?.SetTag("user.id", userId);
            AuthTelemetry.RecordSuccess(new TagList { { AuthTelemetry.SourceTag, AuthTelemetry.OutcomeMiddlewareSource } });
        }
        else
        {
            // When the principal exists and is authenticated but PopulateAuthenticatedContext
            // refused (missing subject), the validator's "failureReason" actually carries the
            // success-path placeholder. Override it with ReasonMissingSubject so telemetry
            // doesn't conflate "no auth attempt" with "auth attempt with unusable claims".
            var effectiveReason = principal?.Identity?.IsAuthenticated == true
                ? AuthTelemetry.ReasonMissingSubject
                : failureReason;
            context.Items[FunctionContextKeys.IsAuthenticated] = false;
            activity?.SetTag("auth.authenticated", false);
            AuthTelemetry.RecordFailure(new TagList { { AuthTelemetry.ReasonTag, effectiveReason } });
        }

        await next(context);
    }

    private bool PopulateAuthenticatedContext(FunctionContext context, ClaimsPrincipal principal)
    {
        var userId = GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
        {
            LogMissingSubjectClaim();
            return false;
        }

        var email = GetEmail(principal);
        var roles = GetRoles(principal);
        var name = GetName(principal);

        // UserId is stored as both string (backward compat) and Guid (for IAuthorizationService)
        context.Items[FunctionContextKeys.UserId] = userId;
        if (Guid.TryParse(userId, out var userGuid))
            context.Items[FunctionContextKeys.UserIdGuid] = userGuid;
        context.Items[FunctionContextKeys.UserEmail] = email ?? string.Empty;
        context.Items[FunctionContextKeys.UserName] = name ?? string.Empty;
        context.Items[FunctionContextKeys.UserRoles] = roles;
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;
        context.Items[FunctionContextKeys.IsAuthenticated] = true;

        // Extract Entra tenant id for downstream services (e.g. JIT provisioning).
        // Non-GUID values are logged and dropped rather than stored as strings.
        var rawTid = principal.FindFirst(TENANT_ID_CLAIM)?.Value;
        if (!string.IsNullOrEmpty(rawTid))
        {
            if (Guid.TryParse(rawTid, out var tenantGuid))
            {
                context.Items[FunctionContextKeys.TenantId] = tenantGuid;
            }
            else
            {
                LogInvalidTenantClaim(rawTid);
            }
        }

        if (string.IsNullOrEmpty(email) && logger.IsEnabled(LogLevel.Warning))
        {
            var claimTypeNames = string.Join("; ", principal.Claims.Select(c => c.Type));
            LogMissingEmailClaim(userId, claimTypeNames);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            var roleList = string.Join(", ", roles);
            LogAuthenticatedUser(userId, roleList);
        }

        return true;
    }

    private async Task<(ClaimsPrincipal? Principal, string FailureReason)> GetClaimsPrincipalAsync(FunctionContext context)
    {
        // Check if ClaimsPrincipal was already set by an upstream middleware or a unit test.
        if (context.Items.TryGetValue(FunctionContextKeys.ClaimsPrincipal, out var existingPrincipal) &&
            existingPrincipal is ClaimsPrincipal principal)
        {
            // Distinct reason from "header missing" so dashboards can tell apart
            // "no auth attempt" vs "auth attempt produced an unauthenticated principal".
            var reason = principal.Identity?.IsAuthenticated == true
                ? AuthTelemetry.ReasonNoPrincipal
                : AuthTelemetry.ReasonUnauthenticatedPrincipal;
            return (principal, reason);
        }

        // Parse + validate the Authorization: Bearer ... header.
        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData is null)
        {
            return (null, AuthTelemetry.ReasonNoPrincipal);
        }

        if (!httpRequestData.Headers.TryGetValues(HttpHeaderNames.Authorization, out var authValues))
        {
            return (null, AuthTelemetry.ReasonNoPrincipal);
        }

        var authHeader = authValues.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(HttpHeaderNames.BearerSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return (null, AuthTelemetry.ReasonNoPrincipal);
        }

        var token = authHeader[HttpHeaderNames.BearerSchemePrefix.Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return (null, AuthTelemetry.ReasonNoPrincipal);
        }

        var validationResult = await validator.ValidateAsync(token, context.CancellationToken);
        return validationResult.Principal is not null
            ? (validationResult.Principal, AuthTelemetry.ReasonNoPrincipal)
            : (null, validationResult.FailureReason ?? AuthTelemetry.ReasonInvalidToken);
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning,
        Message = "No email claim found for user {UserId}. Available claim types: {ClaimTypes}")]
    private partial void LogMissingEmailClaim(string userId, string claimTypes);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug,
        Message = "Authenticated user: {UserId}, Roles: {Roles}")]
    private partial void LogAuthenticatedUser(string userId, string roles);

    [LoggerMessage(EventId = 1107, Level = LogLevel.Warning,
        Message = "Invalid 'tid' claim value '{Value}' — not a GUID; tenant id not stored on context")]
    private partial void LogInvalidTenantClaim(string value);

    [LoggerMessage(EventId = 1109, Level = LogLevel.Warning,
        Message = "Authenticated principal was missing a required subject claim (oid/objectidentifier); treating request as unauthenticated.")]
    private partial void LogMissingSubjectClaim();

    private static string? GetUserId(ClaimsPrincipal principal)
    {
        // Try standard Entra claim first, then alternative
        return principal.FindFirst(OBJECT_ID_CLAIM)?.Value ??
               principal.FindFirst(OBJECT_ID_CLAIM_ALT)?.Value;
    }

    private static string? GetEmail(ClaimsPrincipal principal)
    {
        // Entra External ID uses "emails" claim (array), we take first
        var emailsClaim = principal.FindFirst(EMAIL_CLAIM)?.Value;
        if (!string.IsNullOrEmpty(emailsClaim))
        {
            return emailsClaim;
        }

        // Standard email claims
        return principal.FindFirst(EMAIL_CLAIM_ALT)?.Value ??
               principal.FindFirst(ClaimTypes.Email)?.Value ??
               principal.FindFirst(PREFERRED_USERNAME_CLAIM)?.Value ??
               principal.FindFirst(UPN_CLAIM)?.Value ??
               principal.FindFirst(ClaimTypes.Upn)?.Value;
    }

    private static string? GetName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(NAME_CLAIM)?.Value ??
               principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    private static IReadOnlyList<string> GetRoles(ClaimsPrincipal principal)
    {
        // Collect all role claims
        return principal.FindAll(ROLES_CLAIM)
            .Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToList();
    }
}
