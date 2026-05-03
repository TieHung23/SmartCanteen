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

    [HttpGet("test-logging-database-store-parameter")]
    public async Task<IActionResult> TestLoggingDatabaseStoreParameter(
            [FromQuery] string parameter)
    {
        var result = await mediator.Send(new GetHealthCheckQuery());

        if (result.IsSuccess) return Ok(result);

        return StatusCode(500, result.Error);
    }

    [HttpPost("test-logging-database-store-body")]
    public async Task<IActionResult> TestLoggingDatabaseStoreBody(
        [FromBody] WeatherForecast weatherForecast)
    {
        var result = await mediator.Send(new GetHealthCheckQuery());

        if (result.IsSuccess) return Ok(result);

        return StatusCode(500, result.Error);
    }


    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}