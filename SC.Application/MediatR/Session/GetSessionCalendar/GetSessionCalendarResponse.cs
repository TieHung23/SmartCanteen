namespace SC.Application.MediatR.Session.GetSessionCalendar;

public sealed class GetSessionCalendarResponse
{
    public int Year { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int TotalSessions { get; set; }
    public IReadOnlyList<SessionCalendarDayDto> Days { get; set; } = [];
}

public sealed class SessionCalendarDayDto
{
    public string Date { get; set; } = string.Empty;
    public int SessionCount { get; set; }
}
