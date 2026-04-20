using Microsoft.Extensions.DependencyInjection;
using RajFinancial.Api.Services.AssetService;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Api.Services.ClientManagement;
using RajFinancial.Api.Services.EntityService;
using RajFinancial.Api.Services.UserProfile;
using UserProfileService = RajFinancial.Api.Services.UserProfile.UserProfileService;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for RAJ Financial Planner domain services.
/// </summary>
internal static class ApplicationServicesRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IClientManagementService, ClientManagementService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<
            IEntityService,
            EntityService>();
        return services;
    }
}
