namespace Mms.Application.Features.Reports.Queries;

/// <summary>Danh sách phòng sẽ trống trong N ngày tới vì hợp đồng hết hạn và không có hợp đồng nối tiếp.</summary>
public sealed record GetVacancyForecastQuery(
    int Days = 30,
    int Page = 1,
    int PageSize = 20);
