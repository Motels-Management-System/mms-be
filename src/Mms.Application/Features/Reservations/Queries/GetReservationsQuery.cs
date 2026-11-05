using Mms.Domain.Enums;

namespace Mms.Application.Features.Reservations.Queries;

public sealed record GetReservationsQuery(
    ReservationStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
