using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Notification.RegisterDeviceToken;
using SC.Application.MediatR.Notification.UnregisterDeviceToken;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/device-tokens")]
[Authorize]
public sealed class DeviceTokensController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeviceTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Unregister(
        [FromBody] UnregisterDeviceTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }
}
