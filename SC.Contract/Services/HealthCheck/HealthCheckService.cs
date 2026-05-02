using SC.Contract.Shared;

namespace SC.Contract.Services.HealthCheck;

public class HealthCheckService : IHealthCheckService
{
    public Task<Result> CheckAsync()
    {
        return Task.FromResult(Result.Success("Service is healthy."));
    }
}