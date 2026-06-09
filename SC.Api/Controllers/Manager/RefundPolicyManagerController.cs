using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.RefundPolicy.GetActiveRefundPolicies;
using SC.Application.MediatR.RefundPolicy.Manager.CreateRefundPolicy;
using SC.Application.MediatR.RefundPolicy.Manager.DeleteRefundPolicy;
using SC.Application.MediatR.RefundPolicy.Manager.UpdateRefundPolicy;

namespace SC.Api.Controllers.Manager;

[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/refund-policies")]
[Authorize(Roles = "Manager")]
public sealed class RefundPolicyManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRefundPolicies(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetActiveRefundPoliciesQuery(),
            cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRefundPolicy(
        [FromBody] CreateRefundPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure ? BadRequest(result) : Created(string.Empty, result);
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> UpdateRefundPolicy(
        [FromRoute] string code,
        [FromBody] UpdateRefundPolicyCommand command,
        CancellationToken cancellationToken)
    {
        command.Code = code;
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> DeleteRefundPolicy(
        [FromRoute] string code,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DeleteRefundPolicyCommand(code),
            cancellationToken);
        return result.IsFailure ? NotFound(result) : Ok(result);
    }
}
