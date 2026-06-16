using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Notification.Entity;
using SC.Domain.SharedKernel;

namespace SC.Persistence.Database.Configuration;

public sealed class UserDeviceTokenConfiguration
    : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId)
            .IsRequired();

        builder.HasOne<SC.Domain.Domain.User.User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(token => token.Token)
            .IsRequired()
            .HasMaxLength(DeviceTokenConstraints.TokenMaxLength);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(DeviceTokenConstraints.TokenHashMaxLength);

        builder.Property(token => token.Platform)
            .IsRequired()
            .HasMaxLength(DeviceTokenConstraints.PlatformMaxLength);

        builder.Property(token => token.DeviceId)
            .HasMaxLength(DeviceTokenConstraints.DeviceIdMaxLength);

        builder.Property(token => token.AppVersion)
            .HasMaxLength(DeviceTokenConstraints.AppVersionMaxLength);

        builder.Property(token => token.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(token => token.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(token => token.LastUsedAtUtc)
            .IsRequired();

        builder.Property(token => token.CreatedAtUtc)
            .IsRequired();

        builder.Property(token => token.CreatedBy)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => new
        {
            token.UserId,
            token.IsActive,
            token.IsDeleted
        });

        builder.HasIndex(token => new
        {
            token.UserId,
            token.DeviceId
        });
    }
}
