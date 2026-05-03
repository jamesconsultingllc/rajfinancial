using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Middleware.RateLimit;

/// <summary>
///     Default <see cref="IRateLimitPolicyResolver" /> — caches per function entry point.
/// </summary>
/// <remarks>
///     <para>
///         Method-level <see cref="RateLimitPolicyAttribute" /> wins over class-level. A method
///         with <see cref="RateLimitPolicyKind.Bypass" /> overrides any class-level policy.
///     </para>
///     <para>
///         If the method cannot be resolved (e.g., entry point string unparseable), this
///         resolver returns <see cref="RateLimitPolicy.None" /> — failure to resolve must not
///         silently apply rate limiting to a previously-unprotected endpoint.
///     </para>
/// </remarks>
internal sealed class DefaultRateLimitPolicyResolver(IOptionsMonitor<RateLimitOptions> optionsMonitor)
    : IRateLimitPolicyResolver
{
    private readonly ConcurrentDictionary<string, RateLimitPolicyKind> kindCache = new();
    private readonly ConcurrentDictionary<string, MethodInfo?> methodCache = new();

    public RateLimitPolicy Resolve(FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = optionsMonitor.CurrentValue;

        if (!options.Enabled)
            return RateLimitPolicy.None;

        var entryPoint = context.FunctionDefinition.EntryPoint;
        if (string.IsNullOrEmpty(entryPoint))
            return RateLimitPolicy.None;

        var assemblyPath = context.FunctionDefinition.PathToAssembly;
        var kind = kindCache.GetOrAdd(entryPoint, ep => ResolveKind(ep, assemblyPath));

        return kind switch
        {
            RateLimitPolicyKind.None => RateLimitPolicy.None,
            RateLimitPolicyKind.Bypass => RateLimitPolicy.Bypass,
            RateLimitPolicyKind.AiToolCalling => new RateLimitPolicy(
                RateLimitPolicyKind.AiToolCalling,
                options.AiPolicy.RequestsPerMinute,
                options.AiPolicy.RequestsPerHour,
                options.AiPolicy.FailureMode),
            _ => RateLimitPolicy.None,
        };
    }

    private RateLimitPolicyKind ResolveKind(string entryPoint, string? assemblyPath)
    {
        var method = methodCache.GetOrAdd(entryPoint, ep => ResolveMethod(ep, assemblyPath));
        if (method is null)
            return RateLimitPolicyKind.None;

        var methodAttr = method.GetCustomAttribute<RateLimitPolicyAttribute>();
        if (methodAttr is not null)
            return methodAttr.Kind;

        var classAttr = method.DeclaringType?.GetCustomAttribute<RateLimitPolicyAttribute>();
        return classAttr?.Kind ?? RateLimitPolicyKind.None;
    }

    private static MethodInfo? ResolveMethod(string entryPoint, string? assemblyPath)
    {
        try
        {
            var lastDot = entryPoint.LastIndexOf('.');
            if (lastDot <= 0) return null;

            var typeName = entryPoint[..lastDot];
            var methodName = entryPoint[(lastDot + 1)..];

            var targetType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName))
                .FirstOrDefault(t => t is not null);

            if (targetType is null && !string.IsNullOrEmpty(assemblyPath))
            {
#pragma warning disable S3885
                var assembly = Assembly.LoadFrom(assemblyPath);
#pragma warning restore S3885
                targetType = assembly.GetType(typeName);
            }

            return targetType?.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }
        catch (System.Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
        {
            return null;
        }
    }
}
