using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Services.Contacts;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for <see cref="IContactResolver"/>. Uses a seedable
///     implementation when <c>ENABLE_CONTACT_TEST_SEEDING=true</c>
///     and the environment is non-production; otherwise a placeholder.
/// </summary>
internal static class ContactResolverRegistration
{
    private const string CONTACT_TEST_SEEDING_ENV_VAR = "ENABLE_CONTACT_TEST_SEEDING";

    public static IServiceCollection AddContactResolver(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        if (ShouldUseSeedableResolver(environment))
            services.AddSingleton<IContactResolver, SeedableContactResolver>();
        else
            services.AddSingleton<IContactResolver, PlaceholderContactResolver>();

        return services;
    }

    private static bool ShouldUseSeedableResolver(IHostEnvironment environment)
    {
        var seedingEnabled = string.Equals(
            Environment.GetEnvironmentVariable(CONTACT_TEST_SEEDING_ENV_VAR),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

        if (!seedingEnabled)
            return false;

        if (environment.IsProduction())
            throw new InvalidOperationException(
                $"{CONTACT_TEST_SEEDING_ENV_VAR} must never be enabled in Production.");

        return true;
    }
}
