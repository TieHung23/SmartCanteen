using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Category.CreateCategory;
using SC.Application.MediatR.Category.DeleteCategory;
using SC.Application.MediatR.Category.GetAllCategories;
using SC.Application.MediatR.Category.GetCategoryById;
using SC.Application.MediatR.Category.UpdateCategory;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all categories with pagination and optional search by name
    /// </summary>
    /// <param name="request">Pagination and search parameters</param>
    /// <returns>Paginated list of categories</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllCategories(
        [FromQuery] GetAllCategoriesQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    /// <param name="id">The category ID</param>
    /// <returns>Category details</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById([FromRoute] Guid id)
    {
        var query = new GetCategoryByIdQuery(id);
        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="command">Category creation data</param>
    /// <returns>Created category</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Value!.Id }, result);
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">The category ID</param>
    /// <param name="command">Updated category data</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid id, [FromBody] UpdateCategoryCommand command)
    {
        command.Id = id;
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a category (soft delete)
    /// </summary>
    /// <param name="id">The category ID</param>
    /// <returns>Success or failure result</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory([FromRoute] Guid id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}