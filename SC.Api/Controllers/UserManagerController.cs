using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.User.Manager.GetUserDetail;
using SC.Application.MediatR.User.Manager.GetUsers;
using SC.Application.MediatR.User.Manager.ReactivateUser;
using SC.Application.MediatR.User.Manager.UpdateAccountStatus;
using SC.Domain.Domain.User.Enum;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/manager/users")]
[Authorize(Roles = "Manager")]
public class UserManagerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetManagerUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetManagerUserDetailQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(
        [FromRoute] Guid id,
        [FromBody] UpdateUserAccountStatusRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateUserAccountStatusCommand(id, AccountStatus.Suspended, request?.Reason ?? string.Empty),
            cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/ban")]
    public async Task<IActionResult> Ban(
        [FromRoute] Guid id,
        [FromBody] UpdateUserAccountStatusRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateUserAccountStatusCommand(id, AccountStatus.Banned, request?.Reason ?? string.Empty),
            cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReactivateUserCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    public record UpdateUserAccountStatusRequest(string Reason);
}
