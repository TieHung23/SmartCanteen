using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Category.GetAllCategories;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
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
    
    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetCategoryById([FromRoute] Guid id)
    {
        throw new Exception();
    }
}