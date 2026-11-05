using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Reservations.Commands;

public sealed class ConfirmReservationCommandHandler(
    IAppDbContext dbContext,
    IValidator<ConfirmReservationCommand> validator) : ICommandHandler<ConfirmReservationCommand>
{
    public async Task HandleAsync(ConfirmReservationCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var reservation = await dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new NotFoundException("Giữ phòng", command.Id);

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == reservation.RoomId, ct)
            ?? throw new NotFoundException("Phòng", reservation.RoomId);

        reservation.Confirm();
        room.ChangeStatus(RoomStatus.Reserved);

        await dbContext.SaveChangesAsync(ct);
    }
}
