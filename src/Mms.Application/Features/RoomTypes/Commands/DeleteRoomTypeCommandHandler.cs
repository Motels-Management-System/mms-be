using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Domain.Exceptions;

namespace Mms.Application.Features.RoomTypes.Commands;

public sealed class DeleteRoomTypeCommandHandler(
    IAppDbContext dbContext,
    IValidator<DeleteRoomTypeCommand> validator) : ICommandHandler<DeleteRoomTypeCommand>
{
    public async Task HandleAsync(DeleteRoomTypeCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var roomType = await dbContext.RoomTypes
            .FirstOrDefaultAsync(rt => rt.Id == command.Id, ct);

        if (roomType is null)
            throw new NotFoundException("Không tìm thấy loại phòng.");

        var hasRooms = await dbContext.Rooms
            .AnyAsync(r => r.RoomTypeId == command.Id, ct);

        if (hasRooms)
            throw new DomainException("Không thể xóa loại phòng đang có phòng thuộc loại này.");

        dbContext.RoomTypes.Remove(roomType);
        await dbContext.SaveChangesAsync(ct);
    }
}
