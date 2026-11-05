using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Reservations.Commands;

public sealed class CancelReservationCommandHandler(
    IAppDbContext dbContext,
    IValidator<CancelReservationCommand> validator) : ICommandHandler<CancelReservationCommand>
{
    public async Task HandleAsync(CancelReservationCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var reservation = await dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new NotFoundException("Giữ phòng", command.Id);

        if (!command.IsStaffOrAdmin)
        {
            var ownerUserId = await dbContext.Tenants
                .AsNoTracking()
                .Where(t => t.Id == reservation.TenantId)
                .Select(t => t.UserId)
                .FirstOrDefaultAsync(ct);

            if (ownerUserId != command.CurrentUserId)
                throw new ForbiddenException("Chỉ nhân viên hoặc chủ giữ phòng mới được hủy.");
        }

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == reservation.RoomId, ct)
            ?? throw new NotFoundException("Phòng", reservation.RoomId);

        reservation.Cancel();

        // Chỉ trả phòng về Available khi đang Reserved (đã confirm); giữ Pending thì phòng vẫn Available sẵn.
        if (room.Status == RoomStatus.Reserved)
            room.ChangeStatus(RoomStatus.Available);

        await dbContext.SaveChangesAsync(ct);
    }
}
