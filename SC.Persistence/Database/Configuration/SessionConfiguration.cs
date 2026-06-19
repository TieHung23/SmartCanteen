using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Session.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.FinalizationDeadline);
        builder.Property(x => x.AutoFinalizePolicy).IsRequired().HasDefaultValue(SC.Domain.Domain.Session.Enum.AutoFinalizePolicy.AutoReject);
        builder.Property(x => x.IsFinalized).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.FinalizedAtUtc);

        builder.HasMany(x => x.SessionDishes)
            .WithOne()
            .HasForeignKey("SessionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MealTemplates)
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
