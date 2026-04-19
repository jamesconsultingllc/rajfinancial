namespace RajFinancial.Api.Middleware;

/// <summary>
/// Well-known keys for <see cref="Microsoft.Azure.Functions.Worker.FunctionContext"/> <c>Items</c>
/// values populated by the middleware pipeline and read by downstream middleware, extensions, and functions.
/// </summary>
/// <remarks>
/// Using constants avoids the typo-risk associated with duplicated string literals across production and
/// test code. See AGENT.md "No Magic Strings or Numbers" for the general rule.
/// </remarks>
public static class FunctionContextKeys
{
    /// <summary>User id (string form of the user's GUID); populated by <c>AuthenticationMiddleware</c>.</summary>
    public const string UserId = "UserId";

    /// <summary>User id parsed as <see cref="System.Guid"/>; populated by <c>AuthenticationMiddleware</c>.</summary>
    public const string UserIdGuid = "UserIdGuid";

    /// <summary>Authenticated user's email address; populated by <c>AuthenticationMiddleware</c>.</summary>
    public const string UserEmail = "UserEmail";

    /// <summary>Authenticated user's display name; populated by <c>AuthenticationMiddleware</c>.</summary>
    public const string UserName = "UserName";

    /// <summary>Authenticated user's roles as <c>IReadOnlyList&lt;string&gt;</c>; populated by <c>AuthenticationMiddleware</c>.</summary>
    public const string UserRoles = "UserRoles";

    /// <summary><see cref="System.Security.Claims.ClaimsPrincipal"/>; may be pre-set by upstream middleware or tests, otherwise resolved by <c>AuthenticationMiddleware</c>.</summary>
    public const string ClaimsPrincipal = "ClaimsPrincipal";

    /// <summary><c>bool</c> flag: whether the request is authenticated; populated by <c>AuthenticationMiddleware</c>.</summary>
    public const string IsAuthenticated = "IsAuthenticated";

    /// <summary>Raw request body bytes; populated by <c>ContentNegotiationMiddleware</c>. Preferred for MemoryPack payloads.</summary>
    public const string RequestBodyBytes = "RequestBodyBytes";

    /// <summary>Request <c>Content-Type</c> header value; populated by <c>ContentNegotiationMiddleware</c>.</summary>
    public const string RequestContentType = "RequestContentType";

    /// <summary>UTF-8 string form of the request body; populated by <c>ValidationMiddleware</c> for JSON payloads only.</summary>
    public const string RequestBody = "RequestBody";

    /// <summary>Negotiated response <c>Content-Type</c>; populated by <c>ContentNegotiationMiddleware</c>.</summary>
    public const string ResponseContentType = "ResponseContentType";
}
