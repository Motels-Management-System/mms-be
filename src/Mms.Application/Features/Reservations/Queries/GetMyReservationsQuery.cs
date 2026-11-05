namespace Mms.Application.Features.Reservations.Queries;

public sealed record GetMyReservationsQuery(
    Guid CurrentUserId,
    int Page = 1,
    int PageSize = 20);
