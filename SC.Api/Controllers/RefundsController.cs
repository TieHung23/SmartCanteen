using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Refund.GetMyRefundRequests;
using SC.Application.MediatR.Refund.GetRefundRequestById;
using SC.Application.MediatR.Refund.SubmitRefundRequest;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/refunds")]
[Authorize]
public class RefundsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitRefundRequest(
        [FromForm] Guid orderId,
        [FromForm] string policyCode,
        [FromForm] string description,
        [FromForm] List<IFormFile>? images,
        CancellationToken cancellationToken)
    {
        var imageInputs = images?
            .Select(image => new RefundImageInput(
                image.OpenReadStream(),
                image.FileName,
                image.Length,
                image.ContentType))
            .ToList() ?? [];

        var result = await mediator.Send(
            new SubmitRefundRequestCommand
            {
                OrderId = orderId,
                PolicyCode = policyCode,
                Description = description,
                Images = imageInputs
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetRefundRequestById),
            new { id = result.Value!.Id },
            result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRefundRequests(
        [FromQuery] GetMyRefundRequestsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRefundRequestById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetRefundRequestByIdQuery(id),
            cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
