namespace Mms.Application.Features.MaintenanceRequests.Queries;

/// <summary>Yêu cầu bảo trì của Customer: do chính tài khoản đó tạo hoặc cho phòng mà hồ sơ người thuê của họ từng ở.</summary>
public sealed record GetMyMaintenanceRequestsQuery(
    Guid CurrentUserId,
    int Page = 1,
    int PageSize = 20);
