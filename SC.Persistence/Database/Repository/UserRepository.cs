using Microsoft.EntityFrameworkCore;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User;

namespace SC.Persistence.Database.Repository;

public sealed class UserRepository(SmartCanteenDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
            user => user.Id == id,
            cancellationToken);
    }
}
