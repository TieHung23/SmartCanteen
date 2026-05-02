using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.HealthCheck.GetHealthCheck;

namespace SC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await mediator.Send(new GetHealthCheckQuery());

        if (result.IsSuccess) return Ok(result);

        return StatusCode(500, result.Error);
    }
}