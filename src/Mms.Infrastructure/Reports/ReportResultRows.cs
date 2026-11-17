namespace Mms.Infrastructure.Reports;

// Loại kết quả cho Database.SqlQueryRaw: lớp có tham số khởi tạo rỗng và thuộc tính gán được,
// tên cột trong SQL trùng khớp tên thuộc tính.
internal sealed class OccupancyRow
{
    public string GroupKey { get; set; } = default!;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int RoomCount { get; set; }
    public int RoomDays { get; set; }
    public int OccupiedRoomDays { get; set; }
}

internal sealed class VacancyGapResultRow
{
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = default!;
    public string RoomTypeName { get; set; } = default!;
    public int GapCount { get; set; }
    public int VacancyDays { get; set; }
    public int MaxVacancyDays { get; set; }
}

internal sealed class RenewalRateResultRow
{
    public int EndedContracts { get; set; }
    public int RenewedContracts { get; set; }
    public int ReplacedByNewTenants { get; set; }
    public int LeftVacant { get; set; }
    public int EarlyTerminated { get; set; }
}
