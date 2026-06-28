using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Order.ChangeProposal;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class ChangeProposalsController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> AcceptProposal([FromRoute] Guid id, [FromBody] AcceptChangeProposalCommand command)
    {
        command.ProposalId = id;
        var result = await mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/request-refund")]
    public async Task<IActionResult> RequestRefund([FromRoute] Guid id)
    {
        var command = new RequestRefundFromProposalCommand { ProposalId = id };
        var result = await mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }
}
