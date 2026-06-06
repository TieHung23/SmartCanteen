using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Dish.CreateDish;
using SC.Application.MediatR.Dish.DeleteDish;
using SC.Application.MediatR.Dish.GetAllDishes;
using SC.Application.MediatR.Dish.GetDishById;
using SC.Application.MediatR.Dish.UpdateDish;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
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

    [HttpPost]
    public async Task<IActionResult> CreateDish([FromBody] CreateDishCommand request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetDishById),
            new { id = result.Value!.Id },
            result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDish(
        [FromRoute] Guid id,
        [FromBody] UpdateDishCommand request)
    {
        request.Id = id;
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDish([FromRoute] Guid id)
    {
        var result = await mediator.Send(new DeleteDishCommand(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
