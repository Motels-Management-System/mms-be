namespace Mms.Application.Features.Dashboard.Queries;

/// <summary>Lưới phòng cho dashboard, kèm hợp đồng và người thuê hiện tại của từng phòng.</summary>
public sealed record GetDashboardRoomsQuery(
    int Page = 1,
    int PageSize = 20);
