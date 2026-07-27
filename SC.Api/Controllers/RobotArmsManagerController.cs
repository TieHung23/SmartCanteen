using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.RobotArm.CreateRobotArm;
using SC.Application.MediatR.RobotArm.DeleteRobotArm;
using SC.Application.MediatR.RobotArm.GetAllRobotArms;
using SC.Application.MediatR.RobotArm.GetRobotArmDetail;
using SC.Application.MediatR.RobotArm.SetRobotArmMaintenance;
using SC.Application.MediatR.RobotArm.UpdateRobotArm;

namespace SC.Api.Controllers;

/// <summary>Manager quản lý tay máy: đăng ký, sửa, bảo trì, tháo bỏ, dashboard.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/robot-arms")]
[Authorize(Roles = "Manager,Staff")]
public class RobotArmsManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllRobotArmsQuery(), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRobotArmDetailQuery(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateRobotArmCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateRobotArmCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPatch("{id:guid}/maintenance")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> SetMaintenance(
        [FromRoute] Guid id, [FromQuery] bool inMaintenance, CancellationToken ct)
    {
        var result = await mediator.Send(new SetRobotArmMaintenanceCommand(id, inMaintenance), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteRobotArmCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
