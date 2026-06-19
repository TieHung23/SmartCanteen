using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Robot.CreateServingJob;

namespace SC.Api.Controllers;

/// <summary>
/// Tạo/đẩy job phục vụ cho robot (PUSH/BUFFER). Thường được kích hoạt tự động khi tạo order;
/// endpoint này phục vụ admin/test thủ công.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/robot/serving-jobs")]
[Authorize]
public sealed class RobotServingController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateServingJobCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }
}
