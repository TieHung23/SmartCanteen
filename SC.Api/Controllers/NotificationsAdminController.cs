using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Notification.Admin.CreateNotification;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin/notifications")]
[Authorize(Roles = "Manager")]
public sealed class NotificationsAdminController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result)
            : Ok(result);
    }
}
