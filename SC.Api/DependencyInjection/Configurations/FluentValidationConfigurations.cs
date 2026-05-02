using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using SC.Application.DependencyInjection.Configurations;

namespace SC.Api.DependencyInjection.Configurations;

public static class FluentValidationConfigurations
{
    public static IServiceCollection AddFluentValidationConfigurations(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining(typeof(Assembly));

        return services;
    }
}
