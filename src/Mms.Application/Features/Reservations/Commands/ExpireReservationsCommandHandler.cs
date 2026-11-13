using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Reservations.Commands;

public sealed class ExpireReservationsCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock) : ICommandHandler<ExpireReservationsCommand, int>
{
    /// <summary>Trả về số giữ phòng vừa chuyển sang Expired.</summary>
    public async Task<int> HandleAsync(ExpireReservationsCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;

        var due = await dbContext.Reservations
            .Where(r => (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
                        && r.ExpiresAt <= now)
            .ToListAsync(ct);

        if (due.Count == 0)
            return 0;

        var roomIds = due.Select(r => r.RoomId).Distinct().ToList();
        var roomsById = await dbContext.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, ct);

        var expired = 0;
        foreach (var reservation in due)
        {
            if (!reservation.TryExpire(now))
                continue;

            // Chỉ trả phòng về Available khi phòng đang bị giữ bởi chính reservation này;
            // phòng Occupied thuộc hợp đồng thì giữ nguyên.
            if (roomsById.TryGetValue(reservation.RoomId, out var room) && room.Status == RoomStatus.Reserved)
                room.ChangeStatus(RoomStatus.Available);

            expired++;
        }

        if (expired == 0)
            return 0;

        await ConcurrencyAwareSave.RetryOnceAsync(dbContext, ct);
        return expired;
    }
}
