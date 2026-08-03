using Microsoft.Extensions.DependencyInjection;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Persistence.Database.Repository;
using SC.Persistence.Database.Services;

namespace SC.Persistence.DependencyInjection.Configurations;

public static class Configurations
{
    public static void AddPersistenceConfigurations(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IApiLogRepository, ApiLogRepository>();

        services.AddScoped<IWalletDomainService, WalletDomainService>();
        services.AddScoped<ISessionDishReservationService, SessionDishReservationService>();
        services.AddScoped<IRefundLockService, RefundLockService>();
        services.AddScoped<IRefundAutoCreditService, RefundAutoCreditService>();
        services.AddScoped<IFinalizeSessionService, FinalizeSessionService>();
        services.AddScoped<IChangeProposalExpirationService, ChangeProposalExpirationService>();
    }
}
