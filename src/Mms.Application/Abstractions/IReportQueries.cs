namespace Mms.Application.Abstractions;

/// <summary>
/// Cổng truy vấn báo cáo. Các báo cáo này dùng hàm tổng hợp mà EF Core không dịch được
/// (generate_series, window function) nên viết SQL thuần trong Infrastructure/Reports.
/// </summary>
public interface IReportQueries
{
    /// <summary>Số ngày-phòng có người ở và tổng ngày-phòng trong [from, to), nhóm theo từng bucket.</summary>
    Task<IReadOnlyList<OccupancyBucketRow>> GetOccupancyAsync(
        DateOnly from, DateOnly to, bool groupByRoomType, CancellationToken ct);

    /// <summary>Số ngày trống giữa các hợp đồng liên tiếp của từng phòng, chỉ tính khoảng trống nằm trọn trong [from, to).</summary>
    Task<IReadOnlyList<VacancyGapRow>> GetVacancyGapsAsync(DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>Phân loại các hợp đồng kết thúc trong [from, to): gia hạn, thay người mới, hay bỏ trống.</summary>
    Task<RenewalRateRow> GetRenewalRateAsync(DateOnly from, DateOnly to, CancellationToken ct);
}

/// <param name="GroupKey">Nhãn bucket: tháng (yyyy-MM) hoặc tên loại phòng.</param>
public sealed record OccupancyBucketRow(
    string GroupKey,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int RoomCount,
    int RoomDays,
    int OccupiedRoomDays);

public sealed record VacancyGapRow(
    Guid RoomId,
    string RoomCode,
    string RoomTypeName,
    int GapCount,
    int VacancyDays,
    int MaxVacancyDays);

public sealed record RenewalRateRow(
    int EndedContracts,
    int RenewedContracts,
    int ReplacedByNewTenants,
    int LeftVacant,
    int EarlyTerminated);
