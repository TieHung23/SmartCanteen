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
    [HttpGet]
    public async Task<IActionResult> GetMyProposals()
    {
        var result = await mediator.Send(new GetMyChangeProposalsQuery());

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProposalById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetChangeProposalByIdQuery { ProposalId = id });

        if (result.IsFailure)
            return NotFound(result);

        return Ok(result);
    }

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

    [HttpPost("{id:guid}/request-order-refund")]
    public async Task<IActionResult> RequestOrderRefund([FromRoute] Guid id)
    {
        var command = new RequestOrderRefundFromProposalCommand { ProposalId = id };
        var result = await mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }
}
