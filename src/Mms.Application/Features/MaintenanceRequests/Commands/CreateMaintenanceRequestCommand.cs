namespace Mms.Application.Features.MaintenanceRequests.Commands;

/// <summary>
/// Tạo yêu cầu bảo trì. Nhân viên tạo cho mọi phòng; Customer chỉ tạo cho phòng
/// mình đang ở (hợp đồng Active covering today).
/// </summary>
public sealed record CreateMaintenanceRequestCommand(
    Guid RoomId,
    string Description,
    Guid CurrentUserId,
    bool IsStaffOrAdmin);
