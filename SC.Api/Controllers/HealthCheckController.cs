using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.HealthCheck.GetHealthCheck;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/[controller]")]
public class HealthCheckController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ApiVersion("3.0")]
    public async Task<IActionResult> Get()
    {
        var result = await mediator.Send(new GetHealthCheckQuery());

        if (result.IsSuccess) return Ok(result);

        return StatusCode(500, result.Error);
    }

    [HttpPost("image-test")]
    public async Task<IActionResult> PostImageTest(IFormFile image)
    {
        var result = await mediator.Send(new GetHealthCheckQuery());

        if (result.IsSuccess) return Ok(result);

        return StatusCode(500, result.Error);
    }

    [HttpPost("have-body-test")]
    [ApiVersion("3.0")]
    public async Task<IActionResult> PostHaveBodyTest([FromBody] object body)
    {
        var result = await mediator.Send(new GetHealthCheckQuery());

        if (result.IsSuccess) return Ok(result);

        return StatusCode(500, result.Error);
    }
}