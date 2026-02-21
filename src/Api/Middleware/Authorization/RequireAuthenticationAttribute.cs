namespace RajFinancial.Api.Middleware.Authorization;

/// <summary>
/// Marks an Azure Function as requiring authentication.
/// The <see cref="AuthorizationMiddleware"/> reads this attribute and returns
/// 401 Unauthorized if the request is not authenticated.
/// </summary>
/// <remarks>
/// <para>
/// Apply to individual function methods or to an entire class to require
/// authentication for all functions in that class.
/// </para>
/// <para>
/// <b>OWASP Coverage:</b>
/// <list type="bullet">
///   <item>A01:2025 - Broken Access Control: Deny by default</item>
///   <item>A07:2025 - Authentication Failures: Centralized enforcement</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// [RequireAuthentication]
/// [Function("GetMyProfile")]
/// public async Task&lt;HttpResponseData&gt; Run(
///     [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
///     FunctionContext context) { ... }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RequireAuthenticationAttribute : Attribute;
