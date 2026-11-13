namespace Mms.Application.Features.MeterReadings.Queries;

/// <summary>
/// Danh sách phòng có người ở trong một tháng nhưng chưa được ghi chỉ số.
/// <paramref name="Month"/> theo định dạng yyyy-MM, được validator kiểm tra trước khi parse.
/// </summary>
public sealed record GetMissingMeterReadingsQuery(
    string Month,
    int Page = 1,
    int PageSize = 20);
