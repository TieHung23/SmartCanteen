using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session;

internal static class SessionAvailability
{
    public static bool IsOpenForOrder(SessionAggregateRoot session, DateTimeOffset now)
    {
        return !session.IsDeleted
               && session.IsActive
               && now >= session.AvailableForOrder
               && now <= session.AvailableTo;
    }
}
