using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Notification.AggregateRoot;
using SC.Domain.SharedKernel;

namespace SC.Persistence.Database.Configuration;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientId)
            .IsRequired();

        builder.Property(notification => notification.Type)
            .IsRequired()
            .HasMaxLength(NotificationConstraints.TypeMaxLength);

        builder.Property(notification => notification.Title)
            .IsRequired()
            .HasMaxLength(NotificationConstraints.TitleMaxLength);

        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(NotificationConstraints.MessageMaxLength);

        builder.Property(notification => notification.ReferenceType)
            .HasMaxLength(NotificationConstraints.ReferenceTypeMaxLength);

        builder.Property(notification => notification.ActionUrl)
            .HasMaxLength(NotificationConstraints.ActionUrlMaxLength);

        builder.Property(notification => notification.DataJson)
            .HasMaxLength(NotificationConstraints.DataJsonMaxLength);

        builder.Property(notification => notification.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(notification => notification.CreatedAtUtc)
            .IsRequired();

        builder.Property(notification => notification.CreatedBy)
            .IsRequired();

        builder.HasIndex(notification => new
        {
            notification.RecipientId,
            notification.CreatedAtUtc
        });

        builder.Ignore(notification => notification.DomainEvents);
    }
}
