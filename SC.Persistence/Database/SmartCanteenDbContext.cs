using Microsoft.EntityFrameworkCore;
using SC.Domain.Domain.Category.AggregateRoot;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Meal.AggregateRoot;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Payment.AggregateRoot;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Entity;
using SC.Domain.Domain.User;
using SC.Domain.Domain.Setting.AggregateRoot;
using SC.Domain.Domain.Logging.AggregateRoot;
using SC.Domain.Domain.Verification.AggregateRoot;

namespace SC.Persistence.Database;

public class SmartCanteenDbContext : DbContext
{
    public SmartCanteenDbContext(DbContextOptions<SmartCanteenDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<RefundRequestImage> RefundRequestImages => Set<RefundRequestImage>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartCanteenDbContext).Assembly);
    }
}
