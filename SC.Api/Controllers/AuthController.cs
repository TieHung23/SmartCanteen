using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Auth.ChangePassword;
using SC.Application.MediatR.Auth.ForgotPassword;
using SC.Api.Models.Auth;
using SC.Application.MediatR.Auth.GetCurrentUser;
using SC.Application.MediatR.Auth.GoogleLogin;
using SC.Application.MediatR.Auth.Login;
using SC.Application.MediatR.Auth.Logout;
using SC.Application.MediatR.Auth.Refresh;
using SC.Application.MediatR.Auth.Register;
using SC.Application.MediatR.Auth.ResetPassword;
using SC.Application.MediatR.Auth.UpdateProfile;
using SC.Application.MediatR.Auth.VerifyEmail;
using SC.Contract.Services.Storage;
using SC.Contract.Services.Verification;
using SC.Contract.Shared;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class AuthController(
    IMediator mediator,
    IFileValidator fileValidator,
    IFileUploader fileUploader,
    IValidator<UpdateProfileCommand> updateProfileValidator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> Google([FromBody] GoogleLoginCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var result = await mediator.Send(new GetCurrentUserQuery());
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] UpdateProfileFormRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(
            request.Name,
            null,
            request.DateOfBirth,
            request.MajorOrClass,
            request.PhoneNumber,
            request.Address,
            request.Gender);

        var profileValidation = await updateProfileValidator.ValidateAsync(
            command,
            cancellationToken);

        if (!profileValidation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                profileValidation.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray())));
        }

        if (request.Image is not null)
        {
            if (!request.Image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(Result.Failure(
                    Error.UnsupportedFileFormat,
                    "Avatar must be an image."));
            }

            var fileValidation = fileValidator.Validate(
                request.Image.FileName,
                request.Image.Length,
                request.Image.ContentType);

            if (fileValidation.IsFailure)
            {
                return BadRequest(fileValidation);
            }

            await using var stream = request.Image.OpenReadStream();
            var upload = await fileUploader.UploadAsync(
                stream,
                request.Image.FileName,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(upload.Url))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    Result.Failure(Error.ServerError, "Avatar upload failed."));
            }

            command = command with { ImgUrl = upload.Url };
        }

        return ToUpdateProfileResult(
            await mediator.Send(command, cancellationToken));
    }

    private IActionResult ToUpdateProfileResult(Result<UpdateProfileResponse> result)
    {
        if (result.IsFailure)
        {
            return StatusCode(result.Error?.HttpStatusCode ?? StatusCodes.Status400BadRequest, result);
        }

        return Ok(result);
    }
}
