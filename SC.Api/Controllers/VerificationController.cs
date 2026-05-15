using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SC.Application.MediatR.Verification.GetMyStatus;
using SC.Application.MediatR.Verification.SubmitVerification;
using SC.Contract.Shared;
using SC.Domain.Domain.Verification.Enum;

namespace SC.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class VerificationController(IMediator mediator) : ControllerBase
{
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(
        [FromForm] List<IFormFile> files,
        [FromForm] List<DocumentType> documentTypes,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
            return BadRequest(new { error = "EmptyValue", message = "At least one file is required." });

        if (documentTypes is null || documentTypes.Count != files.Count)
        {
            return BadRequest(new
            {
                error = "InvalidValue",
                message = "documentTypes count must match files count."
            });
        }

        var inputs = new List<VerificationFileInput>(files.Count);
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            inputs.Add(new VerificationFileInput(
                file.OpenReadStream(),
                file.FileName,
                file.Length,
                file.ContentType,
                documentTypes[i]));
        }

        var result = await mediator.Send(new SubmitVerificationCommand(inputs), cancellationToken);
        return Map(result, StatusCodes.Status201Created);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyStatus(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyVerificationStatusQuery(), cancellationToken);
        return Map(result);
    }

    private IActionResult Map<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return StatusCode(successStatusCode, result);

        return result.Error?.ToString() switch
        {
            "Forbidden" => Forbid(),
            "AccountNotActive" => StatusCode(StatusCodes.Status403Forbidden, new { error = "AccountNotActive" }),
            "VerificationAlreadyPending" => Conflict(new { error = "VerificationAlreadyPending" }),
            "FileTooLarge" => StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "FileTooLarge" }),
            "UnsupportedFileFormat" => BadRequest(new { error = "UnsupportedFileFormat" }),
            "EmptyValue" => BadRequest(new { error = "EmptyValue" }),
            "ServerError" => StatusCode(StatusCodes.Status500InternalServerError, new { error = "ServerError" }),
            _ => BadRequest(new { error = result.Error?.ToString() ?? "UnknownError" })
        };
    }
}
