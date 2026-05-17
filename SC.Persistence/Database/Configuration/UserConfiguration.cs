using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.User;

namespace SC.Persistence.Database.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.Property(x => x.ImgUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EmailVerified)
            .IsRequired();

        builder.Property(x => x.StudentId)
            .HasMaxLength(50);

        builder.HasIndex(x => x.StudentId)
            .IsUnique()
            .HasFilter("\"StudentId\" IS NOT NULL");

        builder.Property(x => x.DateOfBirth);

        builder.Property(x => x.MajorOrClass)
            .HasMaxLength(200);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Gender)
            .HasConversion<int>();

        builder.Property(x => x.LastLoginAt);

        builder.OwnsOne(x => x.Balance, money =>
        {
            money.Property(x => x.Amount)
                .HasPrecision(18, 2);

            money.Property(x => x.Currency)
                .HasMaxLength(10);
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
