using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Meal.CreateMeal;
using SC.Application.MediatR.Meal.DeleteMeal;
using SC.Application.MediatR.Meal.GetAllMeals;
using SC.Application.MediatR.Meal.GetMealById;
using SC.Application.MediatR.Meal.UpdateMeal;
using SC.Contract.Shared;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class MealsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all meals with pagination
    /// </summary>
    /// <param name="request">Pagination and filter parameters</param>
    /// <returns>Paginated list of meals</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllMeals([FromQuery] GetAllMealsQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a meal by ID
    /// </summary>
    /// <param name="id">Meal ID</param>
    /// <returns>Meal details with meal settings</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMealById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetMealByIdQuery(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new meal
    /// </summary>
    /// <param name="command">Meal creation details</param>
    /// <returns>Created meal information</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateMeal([FromBody] CreateMealCommand command)
    {
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetMealById), new { id = result.Value!.Id }, result);
    }

    /// <summary>
    /// Update an existing meal
    /// </summary>
    /// <param name="id">Meal ID</param>
    /// <param name="command">Updated meal details</param>
    /// <returns>Updated meal information</returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateMeal([FromRoute] Guid id, [FromBody] UpdateMealCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "ID mismatch between route and request body." });
        }

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a meal
    /// </summary>
    /// <param name="id">Meal ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteMeal([FromRoute] Guid id)
    {
        var result = await mediator.Send(new DeleteMealCommand(id));

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
