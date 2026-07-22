using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Order.GetOrderById;
using SC.Application.MediatR.Order.Manager.GetOrdersBySession;
using SC.Application.MediatR.Order.UpdateOrder;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/orders")]
[Authorize(Roles = "Manager,Staff")]
public sealed class OrderManagerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get a specific order by ID for manager.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id, requireOwner: false), cancellationToken);
        return result.IsFailure ? NotFound(result) : Ok(result);
    }

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

    /// <summary>
    /// Update order status for manager.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id != command.Id)
        {
            command.Id = id;
        }

        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure ? BadRequest(result) : Ok(result);
    }
}
