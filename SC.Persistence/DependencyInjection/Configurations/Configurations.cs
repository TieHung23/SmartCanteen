using Microsoft.Extensions.DependencyInjection;
using SC.Domain.Abstraction.Repositories;
using SC.Persistence.Database.Repository;

namespace SC.Persistence.DependencyInjection.Configurations;

public static class Configurations
{
    public static Type RepositoryType = typeof(RepositoryImp<,>);

    public static void AddPersistenceConfigurations(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepositoryBase<,>), RepositoryType);
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IApiLogRepository, ApiLogRepository>();
    }
}
