using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.WalletTransaction.GetMyWalletTransactions;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/wallet-transactions")]
[Authorize]
public sealed class WalletTransactionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyWalletTransactions(
        [FromQuery] GetMyWalletTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
