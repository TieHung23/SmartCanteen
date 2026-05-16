using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace SC.Api.DependencyInjection.Configurations;

public static class FluentValidationConfigurations
{
    public static IServiceCollection AddFluentValidationConfigurations(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(SC.Application.Assembly.Get);

        return services;
    }
}
