using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.AdminCommandAudit.Entity;

namespace SC.Persistence.Database.Configuration;

public class AdminCommandAuditConfiguration : IEntityTypeConfiguration<AdminCommandAudit>
{
    public void Configure(EntityTypeBuilder<AdminCommandAudit> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CommandType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.RobotArmId);
        builder.HasIndex(x => x.RobotArmId);

        builder.Property(x => x.ParametersJson);
        builder.Property(x => x.Result)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
