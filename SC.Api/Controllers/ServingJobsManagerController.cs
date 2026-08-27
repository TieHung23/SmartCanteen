using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.ServingJobAdmin.FailServingJob;
using SC.Application.MediatR.ServingJobAdmin.GetServingJobs;
using SC.Application.MediatR.ServingJobAdmin.ManualCompleteServingJob;
using SC.Application.MediatR.ServingJobAdmin.RequeueServingJob;

namespace SC.Api.Controllers;

/// <summary>
/// Staff/Manager theo dõi + xử lý job robot: dashboard, cho chạy lại job lỗi,
/// hoàn tất tay (BR: arm fails mid-order, staff manually completes).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/serving-jobs")]
[Authorize(Roles = "Manager,Staff")]
public class ServingJobsManagerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lọc: ?status=Queued|Pushed|Assembling|OnShelf|Collected|Failed|Cancelled
    /// &amp; sessionId (chỉ job của order thuộc session) &amp; take=1..200
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status, [FromQuery] Guid? sessionId, [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetServingJobsQuery(status, sessionId, take), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    /// <summary>Job Failed → về Queued (GIỮ khay — robot gắp tiếp phần thiếu) + ping robot.</summary>
    [HttpPost("{id:guid}/requeue")]
    public async Task<IActionResult> Requeue([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RequeueServingJobCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    /// <summary>Staff tự đặt tay phần còn thiếu → job thành Assembling, khay đủ điều kiện lên kệ.</summary>
    [HttpPost("{id:guid}/manual-complete")]
    public async Task<IActionResult> ManualComplete(
        [FromRoute] Guid id, [FromBody] ManualCompleteServingJobCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    /// <summary>
    /// Staff TUYÊN BỐ job đang dang dở (Queued/Pushed/Assembling) là Failed — dùng khi robot mất
    /// kết nối / đơn kẹt chờ mà cần can thiệp. Mở đường vào Requeue / ManualComplete. Body tuỳ chọn
    /// { "reason": "..." }.
    /// </summary>
    [HttpPost("{id:guid}/fail")]
    public async Task<IActionResult> Fail(
        [FromRoute] Guid id, [FromBody] FailServingJobCommand? command, CancellationToken ct)
    {
        var result = await mediator.Send((command ?? new FailServingJobCommand(id, null)) with { Id = id }, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
