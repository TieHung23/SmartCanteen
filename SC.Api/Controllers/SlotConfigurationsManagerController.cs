using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.SlotConfigurationAdmin.CreateSlotConfiguration;
using SC.Application.MediatR.SlotConfigurationAdmin.DeleteSlotConfiguration;
using SC.Application.MediatR.SlotConfigurationAdmin.GetSlotConfigurations;
using SC.Application.MediatR.SlotConfigurationAdmin.UpdateSlotConfiguration;

namespace SC.Api.Controllers;

/// <summary>Manager cấu hình món↔lane↔tay theo session — nguồn nhãn station/lane robot dùng khi pull.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/slot-configurations")]
[Authorize(Roles = "Manager,Staff")]
public class SlotConfigurationsManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBySession([FromQuery] Guid sessionId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSlotConfigurationsQuery(sessionId), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSlotConfigurationCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateSlotConfigurationCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteSlotConfigurationCommand(id), ct);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? 400, result)
            : Ok(result);
    }
}
