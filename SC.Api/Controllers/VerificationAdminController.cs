using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Verification.Admin.Approve;
using SC.Application.MediatR.Verification.Admin.GetDetail;
using SC.Application.MediatR.Verification.Admin.ListPending;
using SC.Application.MediatR.Verification.Admin.Reject;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin/verifications")]
[Authorize(Roles = "Manager")]
public class VerificationAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListPendingVerificationsQuery query, CancellationToken cancellationToken)
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
        var result = await mediator.Send(new GetVerificationDetailQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveVerificationCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] RejectRequestBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejectVerificationCommand(id, body.Reason), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    public record RejectRequestBody(string Reason);
}
