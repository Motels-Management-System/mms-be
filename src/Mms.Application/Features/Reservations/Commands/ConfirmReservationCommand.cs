namespace Mms.Application.Features.Reservations.Commands;

/// <summary>Nhân viên xác nhận giữ phòng: Pending → Confirmed, phòng → Reserved.</summary>
public sealed record ConfirmReservationCommand(Guid Id);
