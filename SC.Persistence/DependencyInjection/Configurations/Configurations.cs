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
        services.AddScoped<IOrderExpirationService, OrderExpirationService>();
        // Throttle cảnh báo job-Queued-mồ-côi phải SỐNG XUYÊN các lượt sweep (watchdog service là Scoped,
        // tạo mới mỗi 30s) -> đăng ký SINGLETON.
        services.AddSingleton<ServingStuckAlertThrottle>();
        services.AddScoped<IServingJobWatchdogService, ServingJobWatchdogService>();
    }
}
