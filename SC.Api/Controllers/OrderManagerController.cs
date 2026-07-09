using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Order.Manager.GetOrdersBySession;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/orders")]
[Authorize(Roles = "Manager,Staff")]
public sealed class OrderManagerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all orders in a session for manager.
    /// </summary>
    [HttpGet("session/{sessionId:guid}")]
    public async Task<IActionResult> GetOrdersBySession(
        [FromRoute] Guid sessionId,
        [FromQuery] int? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOrdersBySessionQuery(
            sessionId,
            pageNumber,
            pageSize,
            status);

        var result = await mediator.Send(query, cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }
}
