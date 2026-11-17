namespace Mms.Application.Features.Reports.Dtos;

/// <summary>Cách gộp buckets trong báo cáo tỷ lệ lấp đầy.</summary>
public enum OccupancyGroupBy
{
    Month,
    RoomType
}

/// <summary>Một bucket của báo cáo lấp đầy: một tháng hoặc một loại phòng.</summary>
public sealed record OccupancyBucketDto(
    string GroupKey,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int RoomCount,
    int RoomDays,
    int OccupiedRoomDays,
    decimal OccupancyRate);

/// <summary>
/// Tỷ lệ lấp đầy = số ngày-phòng có người ở / tổng số ngày-phòng khả dụng trong khoảng [from, to).
/// </summary>
public sealed record OccupancyReportDto(
    DateOnly From,
    DateOnly To,
    OccupancyGroupBy GroupBy,
    decimal OverallOccupancyRate,
    IReadOnlyList<OccupancyBucketDto> Buckets);
