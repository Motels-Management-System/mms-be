using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.RoomTypes.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.RoomTypes.Commands;

public sealed class CreateRoomTypeCommandHandler(
    IAppDbContext dbContext,
    IValidator<CreateRoomTypeCommand> validator) : ICommandHandler<CreateRoomTypeCommand, RoomTypeDto>
{
    public async Task<RoomTypeDto> HandleAsync(CreateRoomTypeCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var trimmedName = command.Name.Trim();
        var exists = await dbContext.RoomTypes
            .AnyAsync(rt => rt.Name.ToLower() == trimmedName.ToLower(), ct);

        if (exists)
            throw new ConflictException("Tên loại phòng đã tồn tại.");

        var roomType = new RoomType(
            command.Name,
            command.AreaM2,
            command.MaxOccupancy,
            command.ListPrice,
            command.Description);

        dbContext.RoomTypes.Add(roomType);
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
