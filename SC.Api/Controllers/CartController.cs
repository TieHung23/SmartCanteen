using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Cart.ClearCart;
using SC.Application.MediatR.Cart.GetCart;
using SC.Application.MediatR.Cart.UpdateCart;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class CartController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets the cart belonging to the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await mediator.Send(new GetCartQuery());
        return result.IsSuccess
            ? Ok(result)
            : StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
    }

    /// <summary>
    /// Creates or replaces the authenticated user's cart.
    /// </summary>
    /// <remarks>
    /// Send version 0 for a new cart. Otherwise, send the latest version returned by the API.
    /// A stale version returns HTTP 409.
    /// </remarks>
    [HttpPut]
    public async Task<IActionResult> UpdateCart([FromBody] UpdateCartCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(result)
            : StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
    }

    /// <summary>
    /// Clears the authenticated user's cart.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart([FromQuery] long expectedVersion)
    {
        var result = await mediator.Send(new ClearCartCommand(expectedVersion));
        return result.IsSuccess
            ? Ok(result)
            : StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
    }
}
