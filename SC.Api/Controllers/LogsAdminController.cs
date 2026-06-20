using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Logging.Admin.GetLogDetail;
using SC.Application.MediatR.Logging.Admin.ListLogs;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin/logs")]
[Authorize(Roles = "Admin")]
public class LogsAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListApiLogsQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetApiLogDetailQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(result);
        }
        return Ok(result);
    }
}
