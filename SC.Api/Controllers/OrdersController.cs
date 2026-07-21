using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Order.CreateOrder;
using SC.Application.MediatR.Order.DeleteOrder;
using SC.Application.MediatR.Order.GetAllOrders;
using SC.Application.MediatR.Order.GetOrderById;
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
            return StatusCode(
                result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest,
                result);
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
            return StatusCode(
                result.Error?.HttpStatusCode ?? StatusCodes.Status404NotFound,
                result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Checkout the current cart, create an order, and deduct from the user wallet
    /// </summary>
    /// <param name="command">The latest cart version returned by the Cart API</param>
    /// <returns>Created order information with remaining balance</returns>
    /// <remarks>
    /// The server reads and validates the authenticated user's cart. Order creation,
    /// wallet debit, wallet transaction creation, and cart clearing are committed atomically.
    /// A stale cart version returns HTTP 409.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return StatusCode(
                result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest,
                result);
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value!.Id }, result);
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
