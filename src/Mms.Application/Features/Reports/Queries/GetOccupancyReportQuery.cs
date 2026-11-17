using Mms.Application.Features.Reports.Dtos;

namespace Mms.Application.Features.Reports.Queries;

/// <summary>Tỷ lệ lấp đầy trong khoảng [from, to), gộp theo tháng hoặc theo loại phòng.</summary>
public sealed record GetOccupancyReportQuery(
    DateOnly From,
    DateOnly To,
    OccupancyGroupBy GroupBy = OccupancyGroupBy.Month) : IReportPeriodQuery;
