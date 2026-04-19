using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Services.Contacts;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for <see cref="IContactResolver"/>. Uses a seedable
///     implementation when <c>RAJFINANCIAL_ENABLE_CONTACT_TEST_SEEDING=true</c>
///     and the environment is non-production; otherwise a placeholder.
/// </summary>
internal static class ContactResolverRegistration
{
    private const string ContactTestSeedingEnvVar = "RAJFINANCIAL_ENABLE_CONTACT_TEST_SEEDING";

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
            Environment.GetEnvironmentVariable(ContactTestSeedingEnvVar),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

        if (!seedingEnabled)
            return false;

        if (environment.IsProduction())
            throw new InvalidOperationException(
                $"{ContactTestSeedingEnvVar} must never be enabled in Production.");

        return true;
    }
}
