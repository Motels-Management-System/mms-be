using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;

namespace Mms.Application.Features.Rooms.Commands;

public sealed class ChangeRoomStatusCommandHandler(
    IAppDbContext dbContext,
    IValidator<ChangeRoomStatusCommand> validator) : ICommandHandler<ChangeRoomStatusCommand>
{
    public async Task HandleAsync(ChangeRoomStatusCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, ct);

        if (room is null)
            throw new NotFoundException("Phòng", command.RoomId);

        room.ChangeStatus(command.Status);
        await dbContext.SaveChangesAsync(ct);
    }
}
