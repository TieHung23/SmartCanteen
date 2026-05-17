using Microsoft.AspNetCore.Http;
using SC.Domain.Abstraction.Services;
using System.Security.Claims;

namespace SC.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return Guid.Empty;

            var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? httpContext.User.FindFirstValue("sub");

            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }

            return Guid.Empty;
        }
    }
}
