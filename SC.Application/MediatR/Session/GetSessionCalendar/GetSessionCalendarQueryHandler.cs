using System.Globalization;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.GetSessionCalendar;

internal sealed class GetSessionCalendarQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    ILogger<GetSessionCalendarQueryHandler> logger)
    : IQueryHandler<GetSessionCalendarQuery, GetSessionCalendarResponse>
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string Timezone = "Asia/Ho_Chi_Minh";
    private const int MinYear = 2000;
    private const int MaxYear = 2100;

    public async Task<Result<GetSessionCalendarResponse>> Handle(
        GetSessionCalendarQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Year < MinYear || request.Year > MaxYear)
            {
                return Result.Failure<GetSessionCalendarResponse>(
                    Error.InvalidValue,
                    $"Year must be between {MinYear} and {MaxYear}.");
            }

            var timeZone = GetCalendarTimeZone();
            var firstDay = new DateOnly(request.Year, 1, 1);
            var lastDay = new DateOnly(request.Year, 12, 31);
            var totalDays = lastDay.DayNumber - firstDay.DayNumber + 1;

            var sessions = await sessionRepository.FindListAsync(
                session => !session.IsDeleted,
                cancellationToken);

            var countByDate = new Dictionary<DateOnly, int>(totalDays);
            var sessionsInYear = 0;

            foreach (var session in sessions)
            {
                var startDate = ToLocalDate(session.AvailableFrom, timeZone);
                var endDate = ToLocalEndDate(session.AvailableFrom, session.AvailableTo, timeZone);

                if (endDate < firstDay || startDate > lastDay)
                {
                    continue;
                }

                sessionsInYear++;

                var from = startDate < firstDay ? firstDay : startDate;
                var to = endDate > lastDay ? lastDay : endDate;

                for (var date = from; date <= to; date = date.AddDays(1))
                {
                    countByDate[date] = countByDate.GetValueOrDefault(date) + 1;
                }
            }

            var days = new List<SessionCalendarDayDto>(totalDays);
            for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
            {
                days.Add(new SessionCalendarDayDto
                {
                    Date = date.ToString(DateFormat, CultureInfo.InvariantCulture),
                    SessionCount = countByDate.GetValueOrDefault(date)
                });
            }

            return Result.Success(
                new GetSessionCalendarResponse
                {
                    Year = request.Year,
                    Timezone = Timezone,
                    TotalDays = totalDays,
                    TotalSessions = sessionsInYear,
                    Days = days
                },
                "Session calendar retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving session calendar for year {Year}", request.Year);
            return Result.Failure<GetSessionCalendarResponse>(
                Error.ServerError,
                "An error occurred while retrieving the session calendar.");
        }
    }

    private static DateOnly ToLocalDate(DateTimeOffset value, TimeZoneInfo timeZone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, timeZone).Date);
    }

    /// <summary>
    /// A session ending exactly at local midnight belongs to the previous day,
    /// otherwise it would occupy an extra calendar day it is not actually served on.
    /// </summary>
    private static DateOnly ToLocalEndDate(
        DateTimeOffset availableFrom,
        DateTimeOffset availableTo,
        TimeZoneInfo timeZone)
    {
        var localEnd = TimeZoneInfo.ConvertTime(availableTo, timeZone);
        var endDate = DateOnly.FromDateTime(localEnd.Date);

        if (availableTo > availableFrom && localEnd.TimeOfDay == TimeSpan.Zero)
        {
            endDate = endDate.AddDays(-1);
        }

        var startDate = ToLocalDate(availableFrom, timeZone);
        return endDate < startDate ? startDate : endDate;
    }

    private static TimeZoneInfo GetCalendarTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(Timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
