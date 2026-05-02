using SC.Contract.Shared;

namespace SC.Contract.Services.HealthCheck;

public interface IHealthCheckService
{
    Task<Result> CheckAsync();
}