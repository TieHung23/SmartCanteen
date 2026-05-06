using FluentValidation;
using FluentValidation.AspNetCore;

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
