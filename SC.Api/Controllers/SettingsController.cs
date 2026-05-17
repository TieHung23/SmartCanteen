using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Setting.GetAllSettings;
using SC.Application.MediatR.Setting.DeleteSetting;
using SC.Application.MediatR.Setting.GetSettingById;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class SettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllSettings([FromQuery] GetAllSettingsQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSettingById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetSettingByIdQuery(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSetting([FromRoute] Guid id)
    {
        var result = await mediator.Send(new DeleteSettingCommand(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
