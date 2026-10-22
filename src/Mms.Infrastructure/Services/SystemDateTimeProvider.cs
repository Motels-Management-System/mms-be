using Mms.Application.Abstractions;

namespace Mms.Infrastructure.Services;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveTimeZone();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone).DateTime);

    private static TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }       // Linux, macOS, Docker
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } // Windows
    }
}
