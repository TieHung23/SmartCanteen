using SC.Domain.Domain.User;

namespace SC.Domain.Abstraction.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsStudentIdTakenAsync(string studentId, CancellationToken cancellationToken = default);
}
