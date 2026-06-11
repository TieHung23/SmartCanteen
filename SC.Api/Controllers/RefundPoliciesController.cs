using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.RefundPolicy.GetActiveRefundPolicies;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/refund-policies")]
[Authorize]
public class RefundPoliciesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActiveRefundPolicies(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetActiveRefundPoliciesQuery(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
