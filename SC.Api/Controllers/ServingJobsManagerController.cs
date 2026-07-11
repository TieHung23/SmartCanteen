using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.ServingJobAdmin.GetServingJobs;

namespace SC.Api.Controllers;

/// <summary>Manager theo dõi job phục vụ của robot (dashboard: job nào đang chờ/đang ráp/lỗi).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/serving-jobs")]
[Authorize(Roles = "Manager")]
public class ServingJobsManagerController(IMediator mediator) : ControllerBase
{
    /// <summary>Lọc: ?status=Queued|Pushed|Assembling|OnShelf|Collected|Failed|Cancelled &amp; take=1..200</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetServingJobsQuery(status, take), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
