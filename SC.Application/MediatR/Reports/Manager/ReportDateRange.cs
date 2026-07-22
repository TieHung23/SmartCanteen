using System.Globalization;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Reports.Manager;

internal static class ReportDateRange
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string Timezone = "Asia/Ho_Chi_Minh";

    public static Result<ReportDateRangeValue> Create(string? fromValue, string? toValue)
    {
        var timeZone = GetReportTimeZone();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Date);
        var from = today.AddDays(1 - today.Day);
        var to = today;

        if (!string.IsNullOrWhiteSpace(fromValue)
            && !DateOnly.TryParseExact(fromValue, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out from))
        {
            return Result.Failure<ReportDateRangeValue>(
                Error.InvalidValue,
                "From must use yyyy-MM-dd format.");
        }

        if (!string.IsNullOrWhiteSpace(toValue)
            && !DateOnly.TryParseExact(toValue, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out to))
        {
            return Result.Failure<ReportDateRangeValue>(
                Error.InvalidValue,
                "To must use yyyy-MM-dd format.");
        }

        if (from > to)
        {
            return Result.Failure<ReportDateRangeValue>(
                Error.InvalidValue,
                "From must be earlier than or equal to To.");
        }

        return Result.Success(
            new ReportDateRangeValue(
                from,
                to,
                ToUtcStartOfDay(from, timeZone),
                ToUtcStartOfDay(to.AddDays(1), timeZone)),
            null);
    }

    public static bool Contains(DateTimeOffset value, ReportDateRangeValue range)
    {
        return value >= range.StartUtc && value < range.EndUtcExclusive;
    }

    public static ReportRangeDto ToResponse(ReportDateRangeValue range)
    {
        return new ReportRangeDto
        {
            From = range.From.ToString(DateFormat, CultureInfo.InvariantCulture),
            To = range.To.ToString(DateFormat, CultureInfo.InvariantCulture),
            Timezone = Timezone
        };
    }

    private static DateTimeOffset ToUtcStartOfDay(DateOnly date, TimeZoneInfo timeZone)
    {
        var localDateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    private static TimeZoneInfo GetReportTimeZone()
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

public sealed record ReportDateRangeValue(
    DateOnly From,
    DateOnly To,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtcExclusive);

public sealed class ReportRangeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Timezone { get; set; } = ReportDateRange.Timezone;
}
