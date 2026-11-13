namespace Mms.Application.Features.MeterReadings;

/// <summary>
/// Kỳ ghi chỉ số được tính theo tháng và lưu dưới dạng ngày đầu tháng,
/// khớp với cách constructor của entity MeterReading chuẩn hóa giá trị.
/// </summary>
public static class MeterReadingPeriodRules
{
    /// <summary>Định dạng tham số query: yyyy-MM.</summary>
    public const string MonthPattern = @"^\d{4}-(0[1-9]|1[0-2])$";

    public static DateOnly FirstDayOfMonth(DateOnly value) => new(value.Year, value.Month, 1);

    public static DateOnly ParseMonth(string month)
    {
        var parts = month.Split('-');
        return new DateOnly(
            int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
            1);
    }
}
