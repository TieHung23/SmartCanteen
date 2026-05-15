using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Dish.GetAllDishes;
using SC.Application.MediatR.Dish.GetDishById;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class DishesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllDishes(
        [FromQuery] GetAllDishesQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDishById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetDishByIdQuery(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
