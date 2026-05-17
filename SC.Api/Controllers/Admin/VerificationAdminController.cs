using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Verification.Admin.Approve;
using SC.Application.MediatR.Verification.Admin.GetDetail;
using SC.Application.MediatR.Verification.Admin.ListPending;
using SC.Application.MediatR.Verification.Admin.Reject;
using SC.Contract.Shared;

namespace SC.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin/verifications")]
[Authorize(Roles = "Admin")]
public class VerificationAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListPendingVerificationsQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return Map(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetVerificationDetailQuery(id), cancellationToken);
        return Map(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveVerificationCommand(id), cancellationToken);
        return Map(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] RejectRequestBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejectVerificationCommand(id, body.Reason), cancellationToken);
        return Map(result);
    }

    public record RejectRequestBody(string Reason);

    private IActionResult Map(Result result)
    {
        return result.IsSuccess ? Ok(result) : MapFailure(result.Error?.ToString());
    }

    private IActionResult Map<T>(Result<T> result)
    {
        return result.IsSuccess ? Ok(result) : MapFailure(result.Error?.ToString());
    }

    private IActionResult MapFailure(string? errorCode)
    {
        return errorCode switch
        {
            "VerificationNotFound" => NotFound(new { error = errorCode }),
            "VerificationNotPending" => Conflict(new { error = errorCode }),
            "RejectionReasonRequired" => BadRequest(new { error = errorCode }),
            "Forbidden" => Forbid(),
            "ServerError" => StatusCode(StatusCodes.Status500InternalServerError, new { error = errorCode }),
            _ => BadRequest(new { error = errorCode ?? "UnknownError" })
        };
    }
}
