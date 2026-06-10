using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Refund.Manager.ApproveRefundRequest;
using SC.Application.MediatR.Refund.Manager.GetRefundRequestDetail;
using SC.Application.MediatR.Refund.Manager.GetRefundRequests;
using SC.Application.MediatR.Refund.Manager.RejectRefundRequest;

namespace SC.Api.Controllers.Manager;

[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/refunds")]
[Authorize(Roles = "Manager,Admin")]
public class RefundManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRefundRequests(
        [FromQuery] GetRefundRequestsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRefundRequestDetail(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetRefundRequestDetailQuery(id),
            cancellationToken);
        return result.IsFailure ? NotFound(result) : Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveRefundRequest(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ApproveRefundRequestCommand(id),
            cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectRefundRequest(
        [FromRoute] Guid id,
        [FromBody] RejectRefundRequestBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RejectRefundRequestCommand(id, body.Reason),
            cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }

    public sealed record RejectRefundRequestBody(string Reason);
}
