using SC.Contract.Abstraction.Message;
using SC.Contract.Services.HealthCheck;
using SC.Contract.Shared;

namespace SC.Application.MediatR.HealthCheck.GetHealthCheck;

public class GetHealthCheckHandler(IHealthCheckService healthCheckService) : IQueryHandler<GetHealthCheckQuery>
{
    public async Task<Result> Handle(GetHealthCheckQuery request, CancellationToken cancellationToken)
    {
        var result = await healthCheckService.CheckAsync();

        return result;
    }
}