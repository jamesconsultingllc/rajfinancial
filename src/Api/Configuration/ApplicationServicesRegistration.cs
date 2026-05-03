using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RajFinancial.Api.Services.Ai;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Api.Services.Ai.Providers;
using RajFinancial.Api.Services.AssetService;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Api.Services.ClientManagement;
using RajFinancial.Api.Services.EntityService;
using RajFinancial.Api.Services.RateLimit;
using RajFinancial.Api.Services.UserProfile;
using UserProfileService = RajFinancial.Api.Services.UserProfile.UserProfileService;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for RAJ Financial Planner domain services.
/// </summary>
internal static class ApplicationServicesRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IClientManagementService, ClientManagementService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<
            IEntityService,
            EntityService>();

        // AI platform foundation (Tasks #397/#398/#545).
        services.AddRajFinancialAi(configuration);
        services.AddSingleton<IChatClientProvider, AnthropicChatClientProvider>();

        // B1: rate-limit subsystem (AI/tool endpoints).
        services.AddRajFinancialRateLimit(configuration);

        return services;
    }
}
