using Microsoft.EntityFrameworkCore;
using SC.Domain.Domain.Cart.AggregateRoot;
using SC.Domain.Domain.Category.AggregateRoot;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Session.AggregateRoot;
using SC.Domain.Domain.Notification.AggregateRoot;
using SC.Domain.Domain.Notification.Entity;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Payment.AggregateRoot;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Entity;
using SC.Domain.Domain.User;
using SC.Domain.Domain.Setting.AggregateRoot;
using SC.Domain.Domain.Logging.AggregateRoot;
using SC.Domain.Domain.Verification.AggregateRoot;
using SC.Domain.Domain.ServingJob.Entity;
using SC.Domain.Domain.Tray.Entity;
using SC.Domain.Domain.PickupSlot.Entity;
using SC.Domain.Domain.OrderStatusHistory.Entity;
using SC.Domain.Domain.RobotArm.Entity;
using SC.Domain.Domain.SlotConfiguration.Entity;
using SC.Domain.Domain.ShelfStock.Entity;
using SC.Domain.Domain.RobotEventLog.Entity;
using SC.Domain.Domain.AdminCommandAudit.Entity;

namespace SC.Persistence.Database;

public class SmartCanteenDbContext : DbContext
{
    public SmartCanteenDbContext(DbContextOptions<SmartCanteenDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
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

    // Robot arm integration (PUSH/BUFFER serving + pickup)
    public DbSet<ServingJob> ServingJobs => Set<ServingJob>();
    public DbSet<Tray> Trays => Set<Tray>();
    public DbSet<PickupSlot> PickupSlots => Set<PickupSlot>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<RobotArm> RobotArms => Set<RobotArm>();
    public DbSet<SlotConfiguration> SlotConfigurations => Set<SlotConfiguration>();
    public DbSet<ShelfStock> ShelfStocks => Set<ShelfStock>();
    public DbSet<RobotEventLog> RobotEventLogs => Set<RobotEventLog>();
    public DbSet<AdminCommandAudit> AdminCommandAudits => Set<AdminCommandAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartCanteenDbContext).Assembly);
    }
}
