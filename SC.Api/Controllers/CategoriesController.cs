using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Category.CreateCategory;
using SC.Application.MediatR.Category.DeleteCategory;
using SC.Application.MediatR.Category.GetAllCategories;
using SC.Application.MediatR.Category.GetCategoryById;
using SC.Application.MediatR.Category.UpdateCategory;
using SC.Infrastructure.Services.Cloudinary;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController(IMediator mediator, ICloundinaryUpload cloudinaryUpload) : ControllerBase
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

    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        [FromForm] string name,
        [FromForm] string description,
        IFormFile? image)
    {
        string? imgUrl = null;
        if (image is not null)
        {
            await using var stream = image.OpenReadStream();
            var uploadResult = await cloudinaryUpload.UploadFileAsync(stream, image.FileName);
            imgUrl = uploadResult.ViewUrl;
        }

        var command = new CreateCategoryCommand
        {
            Name = name,
            Description = description,
            ImgUrl = imgUrl
        };

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Value!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] Guid id,
        [FromForm] string name,
        [FromForm] string description,
        IFormFile? image)
    {
        string? imgUrl = null;
        if (image is not null)
        {
            await using var stream = image.OpenReadStream();
            var uploadResult = await cloudinaryUpload.UploadFileAsync(stream, image.FileName);
            imgUrl = uploadResult.ViewUrl;
        }

        var command = new UpdateCategoryCommand
        {
            Id = id,
            Name = name,
            Description = description,
            ImgUrl = imgUrl
        };

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

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
