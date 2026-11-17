namespace Mms.Application.Features.Reports.Dtos;

/// <summary>Khoảng trống giữa các hợp đồng liên tiếp của một phòng. Gap 0 ngày là phòng được gia hạn ngay.</summary>
public sealed record RoomVacancyDto(
    Guid RoomId,
    string RoomCode,
    string RoomTypeName,
    int GapCount,
    int VacancyDays,
    decimal AvgVacancyDays,
    int MaxVacancyDays);

/// <summary>Số ngày phòng bỏ trống giữa hai hợp đồng liên tiếp, chỉ tính khoảng trống nằm trọn trong [from, to).</summary>
public sealed record VacancyDurationReportDto(
    DateOnly From,
    DateOnly To,
    int RoomCount,
    int GapCount,
    decimal AvgVacancyDaysPerGap,
    IReadOnlyList<RoomVacancyDto> Items);
