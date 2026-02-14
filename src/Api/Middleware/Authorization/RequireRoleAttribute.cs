namespace RajFinancial.Api.Middleware.Authorization;

/// <summary>
/// Marks an Azure Function as requiring one or more specific app roles.
/// The <see cref="AuthorizationMiddleware"/> reads this attribute and returns
/// 403 Forbidden if the authenticated user does not have any of the required roles.
/// </summary>
/// <remarks>
/// <para>
/// This attribute implies authentication — a function decorated with
/// <c>[RequireRole]</c> does not also need <c>[RequireAuthentication]</c>.
/// If the user is not authenticated, the middleware should return 401 before
/// checking roles.
/// </para>
/// <para>
/// When multiple roles are specified, they are evaluated with OR logic:
/// the user must have <b>at least one</b> of the listed roles.
/// </para>
/// <para>
/// <b>OWASP Coverage:</b>
/// <list type="bullet">
///   <item>A01:2025 - Broken Access Control: Role-based access enforcement</item>
///   <item>A07:2025 - Authentication Failures: Centralized role validation</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// // Single role
/// [RequireRole("Administrator")]
/// [Function("GetAdminDashboard")]
/// public async Task&lt;HttpResponseData&gt; Run(
///     [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
///     FunctionContext context) { ... }
///
/// // Multiple roles (OR logic — user needs at least one)
/// [RequireRole("Administrator", "Client")]
/// [Function("GetSharedResource")]
/// public async Task&lt;HttpResponseData&gt; Run(
///     [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
///     FunctionContext context) { ... }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireRoleAttribute"/> class
    /// with one or more required roles.
    /// </summary>
    /// <param name="roles">
    /// One or more role names required to access the function.
    /// Multiple roles are evaluated with OR logic.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when no roles are specified.</exception>
    public RequireRoleAttribute(params string[] roles)
    {
        if (roles is null || roles.Length == 0)
        {
            throw new ArgumentException("At least one role must be specified.", nameof(roles));
        }

        Roles = roles;
    }

    /// <summary>
    /// Gets the roles required to access the decorated function.
    /// </summary>
    public string[] Roles { get; }
}
