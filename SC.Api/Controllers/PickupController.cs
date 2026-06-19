using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Pickup.AssignPickupSlot;
using SC.Application.MediatR.Pickup.CollectOrder;

namespace SC.Api.Controllers;

/// <summary>
/// Kệ pickup: staff gán khay vào ô; HS quét QR pickup để lấy (dò ngược -&gt; giải phóng ô).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/pickup")]
[Authorize]
public sealed class PickupController(IMediator mediator) : ControllerBase
{
    /// <summary>Staff/sensor gán khay (order) vào 1 ô kệ pickup.</summary>
    [HttpPost("assign")]
    public async Task<IActionResult> Assign(
        [FromBody] AssignPickupSlotCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }

    /// <summary>HS quét QR pickup -&gt; nhận order, giải phóng ô + khay.</summary>
    [HttpPost("collect")]
    public async Task<IActionResult> Collect(
        [FromBody] CollectOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }
}
