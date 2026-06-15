using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Dish.CreateDish;
using SC.Application.MediatR.Dish.DeleteDish;
using SC.Application.MediatR.Dish.GetAllDishes;
using SC.Application.MediatR.Dish.GetDishById;
using SC.Application.MediatR.Dish.UpdateDish;
using SC.Infrastructure.Services.Cloudinary;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class DishesController(IMediator mediator, ICloundinaryUpload cloudinaryUpload) : ControllerBase
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
    public async Task<IActionResult> CreateDish(
        [FromForm] string name,
        [FromForm] string description,
        [FromForm] decimal price,
        [FromForm] Guid categoryId,
        IFormFile? image)
    {
        string? imgUrl = null;
        if (image is not null)
        {
            await using var stream = image.OpenReadStream();
            var uploadResult = await cloudinaryUpload.UploadFileAsync(stream, image.FileName);
            imgUrl = uploadResult.ViewUrl;
        }

        var command = new CreateDishCommand
        {
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            ImgUrl = imgUrl
        };

        var result = await mediator.Send(command);

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
        [FromForm] string name,
        [FromForm] string description,
        [FromForm] decimal price,
        [FromForm] bool isActive,
        [FromForm] Guid categoryId,
        IFormFile? image)
    {
        string? imgUrl = null;
        if (image is not null)
        {
            await using var stream = image.OpenReadStream();
            var uploadResult = await cloudinaryUpload.UploadFileAsync(stream, image.FileName);
            imgUrl = uploadResult.ViewUrl;
        }

        var command = new UpdateDishCommand
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            IsActive = isActive,
            CategoryId = categoryId,
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
