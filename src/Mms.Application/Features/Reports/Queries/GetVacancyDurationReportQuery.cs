namespace Mms.Application.Features.Reports.Queries;

/// <summary>Số ngày trống trung bình giữa các hợp đồng liên tiếp của từng phòng trong [from, to).</summary>
public sealed record GetVacancyDurationReportQuery(
    DateOnly From,
    DateOnly To) : IReportPeriodQuery;
