namespace Mms.Application.Features.Reservations.Commands;

/// <summary>
/// Hủy giữ phòng. Nhân viên hủy được mọi giữ phòng; Customer chỉ hủy giữ phòng của mình
/// (CurrentUserId so với UserId của tenant trong giữ phòng).
/// </summary>
public sealed record CancelReservationCommand(
    Guid Id,
    Guid CurrentUserId,
    bool IsStaffOrAdmin);
