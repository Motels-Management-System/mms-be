namespace Mms.Application.Features.Reports.Queries;

/// <summary>Query báo cáo dùng khoảng thời gian nửa mở [From, To).</summary>
public interface IReportPeriodQuery
{
    DateOnly From { get; }
    DateOnly To { get; }
}
