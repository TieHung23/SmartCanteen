using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.GetSessionCalendar;

public sealed class GetSessionCalendarQuery : IQuery<GetSessionCalendarResponse>
{
    public int Year { get; set; }

    public GetSessionCalendarQuery()
    {
    }

    public GetSessionCalendarQuery(int year)
    {
        Year = year;
    }
}
