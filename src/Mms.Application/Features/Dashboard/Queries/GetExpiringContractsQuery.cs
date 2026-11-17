namespace Mms.Application.Features.Dashboard.Queries;

/// <summary>Hợp đồng đang hiệu lực sẽ hết hạn trong <paramref name="Days"/> ngày tới, sắp theo ngày còn lại tăng dần.</summary>
public sealed record GetExpiringContractsQuery(
    int Days = 30,
    int Page = 1,
    int PageSize = 20);
