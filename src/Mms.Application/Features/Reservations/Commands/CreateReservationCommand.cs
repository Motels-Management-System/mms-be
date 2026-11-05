namespace Mms.Application.Features.Reservations.Commands;

/// <summary>
/// Tạo giữ phòng. Customer giữ cho chính mình (TenantId null, lấy từ hồ sơ liên kết UserId);
/// Staff/Admin giữ hộ nên truyền TenantId của người thuê.
/// </summary>
public sealed record CreateReservationCommand(
    Guid RoomId,
    Guid? TenantId,
    Guid CurrentUserId,
    bool IsStaffOrAdmin);
