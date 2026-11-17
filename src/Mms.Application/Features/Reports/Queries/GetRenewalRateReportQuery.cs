namespace Mms.Application.Features.Reports.Queries;

/// <summary>Tỷ lệ gia hạn và tỷ lệ quay vòng của các hợp đồng kết thúc trong [from, to).</summary>
public sealed record GetRenewalRateReportQuery(
    DateOnly From,
    DateOnly To) : IReportPeriodQuery;
