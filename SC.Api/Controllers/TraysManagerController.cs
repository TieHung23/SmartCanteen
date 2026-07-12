using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Tray.CreateTrays;
using SC.Application.MediatR.Tray.ForceReleaseTray;
using SC.Application.MediatR.Tray.GetAllTrays;
using SC.Application.MediatR.Tray.RetireTray;

namespace SC.Api.Controllers;

/// <summary>Manager quản lý pool khay: đăng ký (đơn/bulk), xem pool, gỡ kẹt, loại khay hỏng.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/trays")]
[Authorize(Roles = "Manager,Staff")]
public class TraysManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllTraysQuery(), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateTraysCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPatch("{id:guid}/force-release")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ForceRelease([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ForceReleaseTrayCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPatch("{id:guid}/retire")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Retire([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RetireTrayCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
