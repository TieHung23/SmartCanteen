using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Order.CreateOrder;
using SC.Application.MediatR.Order.DeleteOrder;
using SC.Application.MediatR.Order.GetAllOrders;
using SC.Application.MediatR.Order.GetOrderById;
using SC.Application.MediatR.Order.UpdateOrder;
using SC.Contract.Shared;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all orders for the current user with pagination
    /// </summary>
    /// <param name="request">Pagination and filter parameters</param>
    /// <returns>Paginated list of user's orders</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllOrders([FromQuery] GetAllOrdersQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a specific order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details with items</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new order and deduct from user wallet
    /// </summary>
    /// <param name="command">Order details with items</param>
    /// <returns>Created order information with remaining balance</returns>
    /// <remarks>
    /// The total price is calculated from all items and deducted from the user's wallet.
    /// The request will fail if the user has insufficient balance.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value!.Id }, result);
    }

    /// <summary>
    /// Update order status
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="command">New order status (0=Pending, 1=ReadyForPickup, 2=Completed, 3=Cancelled)</param>
    /// <returns>Updated order information</returns>
    /// <remarks>
    /// Status values:
    /// - 0: Pending
    /// - 1: ReadyForPickup
    /// - 2: Completed
    /// - 3: Cancelled
    /// </remarks>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateOrder([FromRoute] Guid id, [FromBody] UpdateOrderCommand command)
    {
        if (id != command.Id)
        {
            command.Id = id;
        }

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder([FromRoute] Guid id)
    {
        var result = await mediator.Send(new DeleteOrderCommand(id));

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
