using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.PickupSlotAdmin.CreatePickupSlots;
using SC.Application.MediatR.PickupSlotAdmin.ForceClearPickupSlot;
using SC.Application.MediatR.PickupSlotAdmin.GetAllPickupSlots;
using SC.Application.MediatR.PickupSlotAdmin.RetirePickupSlot;

namespace SC.Api.Controllers;

/// <summary>Manager quản lý kệ pickup: đăng ký ô, xem kệ, dọn ô no-show (Order → Expired), loại ô hỏng.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/pickup-slots")]
[Authorize(Roles = "Manager")]
public class PickupSlotsManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllPickupSlotsQuery(), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePickupSlotsCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPatch("{id:guid}/force-clear")]
    public async Task<IActionResult> ForceClear([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ForceClearPickupSlotCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPatch("{id:guid}/retire")]
    public async Task<IActionResult> Retire([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RetirePickupSlotCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
