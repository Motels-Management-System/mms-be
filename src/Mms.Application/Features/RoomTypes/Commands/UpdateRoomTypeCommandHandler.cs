using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.RoomTypes.Dtos;

namespace Mms.Application.Features.RoomTypes.Commands;

public sealed class UpdateRoomTypeCommandHandler(
    IAppDbContext dbContext,
    IValidator<UpdateRoomTypeCommand> validator) : ICommandHandler<UpdateRoomTypeCommand, RoomTypeDto>
{
    public async Task<RoomTypeDto> HandleAsync(UpdateRoomTypeCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var roomType = await dbContext.RoomTypes
            .FirstOrDefaultAsync(rt => rt.Id == command.Id, ct);

        if (roomType is null)
            throw new NotFoundException("Không tìm thấy loại phòng.");

        var trimmedName = command.Name.Trim();
        var duplicate = await dbContext.RoomTypes
            .AnyAsync(rt => rt.Id != command.Id && rt.Name.ToLower() == trimmedName.ToLower(), ct);

        if (duplicate)
            throw new ConflictException("Tên loại phòng đã tồn tại.");

        roomType.Update(
            command.Name,
            command.AreaM2,
            command.MaxOccupancy,
            command.ListPrice,
            command.Description);

        await dbContext.SaveChangesAsync(ct);

        return new RoomTypeDto(
            roomType.Id,
            roomType.Name,
            roomType.AreaM2,
            roomType.MaxOccupancy,
            roomType.ListPrice,
            roomType.Description,
            roomType.CreatedAt,
            roomType.UpdatedAt);
    }
}
