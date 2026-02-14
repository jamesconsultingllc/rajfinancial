using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.Api.Middleware.Authorization;

/// <summary>
/// Middleware that enforces authorization by reading <see cref="RequireAuthenticationAttribute"/>
/// and <see cref="RequireRoleAttribute"/> from the target function method or class.
/// </summary>
/// <remarks>
/// <para>
/// This middleware runs after <see cref="AuthenticationMiddleware"/> and uses the user context
/// it established in <see cref="FunctionContext.Items"/> to make authorization decisions.
/// </para>
/// <para>
/// <b>Authorization Flow:</b>
/// <list type="number">
///   <item>Resolve the target function method via <see cref="FunctionDefinition.EntryPoint"/>.</item>
///   <item>Check for <see cref="RequireRoleAttribute"/> (method-level, then class-level).
///         If found, verify authentication first, then verify the user has at least one required role.</item>
///   <item>If no role attribute, check for <see cref="RequireAuthenticationAttribute"/>
///         (method-level, then class-level). If found, verify authentication.</item>
///   <item>Functions without either attribute remain public.</item>
/// </list>
/// </para>
/// <para>
/// <b>OWASP Coverage:</b>
/// <list type="bullet">
///   <item>A01:2025 - Broken Access Control: Centralized attribute-based enforcement</item>
///   <item>A07:2025 - Authentication Failures: Deny-by-default for decorated functions</item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b>
/// Authorization failures throw <see cref="UnauthorizedException"/> (401) or
/// <see cref="ForbiddenException"/> (403), which are caught and formatted by
/// <see cref="ExceptionMiddleware"/>.
/// </para>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public class AuthorizationMiddleware(ILogger<AuthorizationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private readonly ConcurrentDictionary<string, MethodInfo?> methodCache = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var targetMethod = GetTargetMethod(context);

        if (targetMethod is not null)
        {
            // Check [RequireRole] first — it implies authentication
            var requireRole = targetMethod.GetCustomAttribute<RequireRoleAttribute>()
                ?? targetMethod.DeclaringType?.GetCustomAttribute<RequireRoleAttribute>();

            if (requireRole is not null)
            {
                EnsureAuthenticated(context);
                EnsureHasRole(context, requireRole.Roles);

                logger.LogDebug(
                    "Authorization passed for {Function}: user has required role",
                    context.FunctionDefinition?.Name ?? "unknown");
            }
            else
            {
                // Check [RequireAuthentication] — authentication only, no role check
                var requireAuth = targetMethod.GetCustomAttribute<RequireAuthenticationAttribute>()
                    ?? targetMethod.DeclaringType?.GetCustomAttribute<RequireAuthenticationAttribute>();

                if (requireAuth is not null)
                {
                    EnsureAuthenticated(context);

                    logger.LogDebug(
                        "Authentication verified for {Function}",
                        context.FunctionDefinition?.Name ?? "unknown");
                }
            }
        }

        await next(context);
    }

    /// <summary>
    /// Resolves the target function method from <see cref="FunctionDefinition.EntryPoint"/>
    /// using reflection. Results are cached per entry point for performance.
    /// </summary>
    private MethodInfo? GetTargetMethod(FunctionContext context)
    {
        var entryPoint = context.FunctionDefinition?.EntryPoint;
        if (string.IsNullOrEmpty(entryPoint))
            return null;

        var assemblyPath = context.FunctionDefinition?.PathToAssembly;
        return methodCache.GetOrAdd(entryPoint, ep => ResolveMethod(ep, assemblyPath));
    }

    /// <summary>
    /// Parses the entry point string ("Namespace.Class.Method") and resolves the MethodInfo
    /// by searching loaded assemblies, falling back to <see cref="Assembly.LoadFrom"/>.
    /// </summary>
    private MethodInfo? ResolveMethod(string entryPoint, string? assemblyPath)
    {
        try
        {
            var lastDot = entryPoint.LastIndexOf('.');
            if (lastDot <= 0) return null;

            var typeName = entryPoint[..lastDot];
            var methodName = entryPoint[(lastDot + 1)..];

            // Search all loaded assemblies first (handles both production and test scenarios)
            var targetType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName))
                .FirstOrDefault(t => t is not null);

            // Fall back to loading from PathToAssembly if type not found
            if (targetType is null && !string.IsNullOrEmpty(assemblyPath))
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                targetType = assembly.GetType(typeName);
            }

            return targetType?.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }
        catch (System.Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve target method for entry point: {EntryPoint}", entryPoint);
            return null;
        }
    }

    /// <summary>
    /// Verifies the current request is authenticated by checking <c>IsAuthenticated</c>
    /// in <see cref="FunctionContext.Items"/>.
    /// </summary>
    /// <exception cref="UnauthorizedException">Thrown when the user is not authenticated.</exception>
    private static void EnsureAuthenticated(FunctionContext context)
    {
        var isAuthenticated = context.Items.TryGetValue("IsAuthenticated", out var authValue)
                              && authValue is true;

        if (!isAuthenticated)
            throw new UnauthorizedException("Authentication is required to access this resource.");
    }

    /// <summary>
    /// Verifies the authenticated user has at least one of the required roles (OR logic)
    /// by checking <c>UserRoles</c> in <see cref="FunctionContext.Items"/>.
    /// </summary>
    /// <exception cref="ForbiddenException">Thrown when the user lacks all required roles.</exception>
    private static void EnsureHasRole(FunctionContext context, string[] requiredRoles)
    {
        var userRoles = context.Items.TryGetValue("UserRoles", out var rolesObj)
            ? rolesObj as IReadOnlyList<string> ?? []
            : [];

        // OR logic: user needs at least one of the required roles
        var hasRole = requiredRoles.Any(required =>
            userRoles.Any(userRole =>
                string.Equals(userRole, required, StringComparison.OrdinalIgnoreCase)));

        if (!hasRole)
            throw new ForbiddenException(
                $"Access denied. Required role(s): {string.Join(", ", requiredRoles)}");
    }
}
