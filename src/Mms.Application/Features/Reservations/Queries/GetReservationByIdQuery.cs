namespace Mms.Application.Features.Reservations.Queries;

public sealed record GetReservationByIdQuery(
    Guid Id,
    Guid CurrentUserId,
    bool IsStaffOrAdmin);
