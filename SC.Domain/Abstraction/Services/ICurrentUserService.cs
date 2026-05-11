namespace SC.Domain.Abstraction.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
}
