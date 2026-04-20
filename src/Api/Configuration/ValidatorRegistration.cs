using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RajFinancial.Api.Validators;
using RajFinancial.Api.Validators.Entities;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for FluentValidation request validators.
/// </summary>
internal static class ValidatorRegistration
{
    public static IServiceCollection AddApplicationValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateAssetRequest>, CreateAssetRequestValidator>();
        services.AddScoped<IValidator<UpdateAssetRequest>, UpdateAssetRequestValidator>();
        services.AddScoped<IValidator<AssignClientRequest>, AssignClientRequestValidator>();
        services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();
        services.AddScoped<IValidator<CreateEntityRequest>, CreateEntityRequestValidator>();
        services.AddScoped<IValidator<UpdateEntityRequest>, UpdateEntityRequestValidator>();
        services.AddScoped<IValidator<CreateEntityRoleRequest>, CreateEntityRoleRequestValidator>();
        return services;
    }
}
