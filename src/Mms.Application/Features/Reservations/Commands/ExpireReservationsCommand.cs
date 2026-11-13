namespace Mms.Application.Features.Reservations.Commands;

/// <summary>
/// Đánh dấu hết hạn các giữ phòng Pending/Confirmed đã quá ExpiresAt và trả phòng về Available.
/// Lệnh nội bộ, không có dữ liệu người dùng nên không cần validator.
/// </summary>
public sealed record ExpireReservationsCommand;
