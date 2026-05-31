using Microsoft.AspNetCore.Mvc;
using SC.Contract.Shared;

namespace SC.Api.Extensions;

/// <summary>
/// Bridges the domain <see cref="Result"/> / <see cref="Result{TValue}"/> types and HTTP responses.
/// On success, returns the Result envelope with the caller-chosen status code (200 by default).
/// On failure, returns <c>{ error, message }</c> with the HTTP status declared on the <see cref="Error"/>.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return new ObjectResult(result) { StatusCode = successStatusCode };

        return BuildFailure(result.Error, result.Message);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return new ObjectResult(result) { StatusCode = successStatusCode };

        return BuildFailure(result.Error, result.Message);
    }

    private static ObjectResult BuildFailure(Error? error, string? message)
    {
        var resolved = error ?? Error.ServerError;
        return new ObjectResult(new
        {
            error = resolved.Code,
            message = string.IsNullOrEmpty(message) ? resolved.Message : message
        })
        {
            StatusCode = resolved.HttpStatusCode
        };
    }
}
