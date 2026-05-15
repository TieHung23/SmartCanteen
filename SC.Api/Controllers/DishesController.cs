using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Dish.GetAllDishes;

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
}
