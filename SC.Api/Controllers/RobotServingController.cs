using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Robot.CreateServingJob;
using SC.Application.MediatR.Robot.PullNextJob;

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

    /// <summary>
    /// Robot service KÉO job kế tiếp (hybrid pull). BE gán khay trống (lazy) + claim job.
    /// 200 + job nếu có việc; 204 nếu chưa có việc hoặc hết khay.
    /// </summary>
    [HttpPost("next")]
    public async Task<IActionResult> PullNext(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PullNextJobCommand(), cancellationToken);
        if (result.IsFailure)
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        return result.Value!.Job is null ? NoContent() : Ok(result);
    }
}
