using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Session.CreateSession;
using SC.Application.MediatR.Session.DeleteSession;
using SC.Application.MediatR.Session.GetAllSessions;
using SC.Application.MediatR.Session.GetSessionById;
using SC.Application.MediatR.Session.UpdateSession;
using SC.Contract.Shared;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class SessionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all sessions with pagination
    /// </summary>
    /// <param name="request">Pagination and filter parameters</param>
    /// <returns>Paginated list of sessions</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllSessions([FromQuery] GetAllSessionsQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a session by ID
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session details with meal settings</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSessionById([FromRoute] Guid id)
    {
        var result = await mediator.Send(new GetSessionByIdQuery(id));

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new session
    /// </summary>
    /// <param name="command">Session creation details</param>
    /// <returns>Created session information</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionCommand command)
    {
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetSessionById), new { id = result.Value!.Id }, result);
    }

    /// <summary>
    /// Update an existing session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="command">Updated session details</param>
    /// <returns>Updated session information</returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateSession([FromRoute] Guid id, [FromBody] UpdateSessionCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "ID mismatch between route and request body." });
        }

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSession([FromRoute] Guid id)
    {
        var result = await mediator.Send(new DeleteSessionCommand(id));

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
