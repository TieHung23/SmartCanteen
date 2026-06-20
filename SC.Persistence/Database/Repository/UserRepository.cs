using Microsoft.EntityFrameworkCore;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User;

namespace SC.Persistence.Database.Repository;

public class UserRepository(SmartCanteenDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().FirstOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> IsStudentIdTakenAsync(string studentId, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().AnyAsync(u => u.StudentId == studentId, cancellationToken);
    }
}
