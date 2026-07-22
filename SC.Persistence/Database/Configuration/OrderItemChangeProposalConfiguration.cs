using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Order.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class OrderItemChangeProposalConfiguration : IEntityTypeConfiguration<OrderItemChangeProposal>
{
    public void Configure(EntityTypeBuilder<OrderItemChangeProposal> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.CurrentDishId).IsRequired();

        builder.Property(x => x.SuggestedDishId);

        builder.Property(x => x.SelectedDishId);

        builder.Property(x => x.IsRequiredItem).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.RequiredCategoryId);

        builder.Property(x => x.ProposalStatus).IsRequired().HasDefaultValue(SC.Domain.Domain.Order.Enum.ChangeProposalStatus.WaitingResponse);

        builder.Property(x => x.RespondedAtUtc);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
