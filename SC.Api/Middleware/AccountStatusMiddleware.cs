using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SC.Contract.Shared;
using SC.Domain.Domain.User.Enum;
using SC.Persistence.Database;

namespace SC.Api.Middleware;

public class AccountStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SmartCanteenDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userIdText = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdText, out var userId))
        {
            await next(context);
            return;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, context.RequestAborted);

        if (user is null || user.IsDeleted)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                Result.Failure(Error.UserNotFound, "Account no longer exists."),
                context.RequestAborted);
            return;
        }

        if (user.Status == AccountStatus.Banned)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    isSuccess = false,
                    errorCode = Error.AccountBanned.Code,
                    message = "This account has been banned.",
                    reason = user.StatusReason
                },
                context.RequestAborted);
            return;
        }

        if (user.Status == AccountStatus.Suspended)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    isSuccess = false,
                    errorCode = Error.AccountSuspended.Code,
                    message = "This account has been suspended.",
                    reason = user.StatusReason
                },
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
