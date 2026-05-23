using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Auth.GetCurrentUser;
using SC.Application.MediatR.Auth.GoogleLogin;
using SC.Application.MediatR.Auth.Login;
using SC.Application.MediatR.Auth.Logout;
using SC.Application.MediatR.Auth.Refresh;
using SC.Application.MediatR.Auth.Register;
using SC.Application.MediatR.Auth.VerifyEmail;
using SC.Contract.Shared;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await mediator.Send(command);
        return Map(result, successStatusCode: StatusCodes.Status201Created);
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await mediator.Send(new VerifyEmailCommand(token));
        return Map(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        return Map(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await mediator.Send(command);
        return Map(result);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> Google([FromBody] GoogleLoginCommand command)
    {
        var result = await mediator.Send(command);
        return Map(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await mediator.Send(command);
        return Map(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var result = await mediator.Send(new GetCurrentUserQuery());
        return Map(result);
    }

    private IActionResult Map(Result result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return StatusCode(successStatusCode, result);

        return MapFailure(result.Error?.ToString());
    }

    private IActionResult Map<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return StatusCode(successStatusCode, result);

        return MapFailure(result.Error?.ToString());
    }

    private IActionResult MapFailure(string? errorCode)
    {
        return errorCode switch
        {
            "EmailAlreadyExists" => Conflict(new { error = errorCode }),
            "StudentIdAlreadyUsed" => Conflict(new { error = errorCode }),
            "InvalidCredentials" => Unauthorized(new { error = errorCode }),
            "EmailNotVerified" => Unauthorized(new { error = errorCode }),
            "AccountSuspended" => Unauthorized(new { error = errorCode }),
            "AccountNotActive" => Unauthorized(new { error = errorCode }),
            "InvalidOrExpiredToken" => BadRequest(new { error = errorCode }),
            "InvalidRefreshToken" => Unauthorized(new { error = errorCode }),
            "GoogleTokenInvalid" => Unauthorized(new { error = errorCode }),
            "GoogleEmailNotVerified" => Unauthorized(new { error = errorCode }),
            "NonFptGoogleAccount" => StatusCode(StatusCodes.Status403Forbidden, new { error = errorCode }),
            "Forbidden" => Forbid(),
            "ServerError" => StatusCode(StatusCodes.Status500InternalServerError, new { error = errorCode }),
            _ => BadRequest(new { error = errorCode ?? "UnknownError" })
        };
    }
}
