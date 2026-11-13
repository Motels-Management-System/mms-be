namespace Mms.Infrastructure.Services;

/// <summary>Múi giờ Việt Nam, dùng chung cho clock và lịch chạy background job.</summary>
internal static class VietnamTime
{
    public static TimeZoneInfo Zone { get; } = ResolveZone();

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    private static TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }       // Linux, macOS, Docker
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } // Windows
    }
}
