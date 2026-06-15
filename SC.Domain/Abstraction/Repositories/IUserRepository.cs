using SC.Domain.Domain.User;

namespace SC.Domain.Abstraction.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
