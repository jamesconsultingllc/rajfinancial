using System.Security.Claims;

namespace RajFinancial.Api.Services.Auth;

/// <summary>
///     Validates an incoming bearer JWT and returns the resulting
///     <see cref="ClaimsPrincipal"/>, or <c>null</c> if validation fails.
/// </summary>
/// <remarks>
///     Two implementations exist:
///     <list type="bullet">
///         <item>
///             <see cref="JwtBearerValidator"/> — production validator that performs signature,
///             issuer, audience and lifetime validation against the Entra External ID
///             OpenID Connect discovery document.
///         </item>
///         <item>
///             <see cref="LocalUnsignedJwtValidator"/> — local-only validator used by
///             integration tests running against an unsigned test harness. Gated by the
///             <c>AUTH__USE_UNSIGNED_LOCAL_VALIDATOR</c> environment variable and rejected
///             at startup if <c>WEBSITE_SITE_NAME</c> is set (i.e. running on App Service).
///         </item>
///     </list>
/// </remarks>
public interface IJwtBearerValidator
{
    /// <summary>
    ///     Validates the supplied bearer token.
    /// </summary>
    /// <param name="bearerToken">
    ///     Raw JWT (the value that follows <c>Bearer </c> in the <c>Authorization</c> header).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A <see cref="ClaimsPrincipal"/> whose <c>Identity.IsAuthenticated</c> is <c>true</c>
    ///     on success; otherwise <c>null</c>. Implementations must not throw on validation
    ///     failure — return <c>null</c> so the middleware can emit a 401 via the standard chain.
    /// </returns>
    Task<ClaimsPrincipal?> ValidateAsync(string bearerToken, CancellationToken cancellationToken);
}
