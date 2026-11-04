using Lending.Application.Abstractions;

namespace Lending.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException("A time zone identifier is required.", nameof(timeZoneId));
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(UtcNow, timeZone);
        return DateOnly.FromDateTime(localNow.DateTime);
    }
}
