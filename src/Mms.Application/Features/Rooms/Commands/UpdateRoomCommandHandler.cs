using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Application.Features.RoomTypes.Dtos;

namespace Mms.Application.Features.Rooms.Commands;

public sealed class UpdateRoomCommandHandler(
    IAppDbContext dbContext,
    IValidator<UpdateRoomCommand> validator) : ICommandHandler<UpdateRoomCommand, RoomDetailDto>
{
    public async Task<RoomDetailDto> HandleAsync(UpdateRoomCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct);

        if (room is null)
            throw new NotFoundException("Phòng", command.Id);

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        var codeExists = await dbContext.Rooms
            .AnyAsync(r => r.Id != command.Id && r.Code == normalizedCode, ct);

        if (codeExists)
            throw new ConflictException("Mã phòng đã tồn tại.");

        var roomType = await dbContext.RoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == command.RoomTypeId, ct);

        if (roomType is null)
            throw new NotFoundException("Loại phòng", command.RoomTypeId);

        room.Update(command.Code, command.Floor, command.RoomTypeId);
        await dbContext.SaveChangesAsync(ct);

        // Xếp hạng trên cột của entity rồi mới dựng DTO: OrderBy trên DTO không dịch được sang SQL.
        var amenityRows = await dbContext.RoomAmenities
            .AsNoTracking()
            .Where(ra => ra.RoomId == room.Id)
            .Join(
                dbContext.Amenities.AsNoTracking(),
                ra => ra.AmenityId,
                a => a.Id,
                (ra, a) => new { a.Id, a.Name, a.CreatedAt, a.UpdatedAt })
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        var amenities = amenityRows
            .Select(a => new AmenityDto(a.Id, a.Name, a.CreatedAt, a.UpdatedAt))
            .ToList();

        var roomTypeDto = new RoomTypeDto(
            roomType.Id,
            roomType.Name,
            roomType.AreaM2,
            roomType.MaxOccupancy,
            roomType.ListPrice,
            roomType.Description,
            roomType.CreatedAt,
            roomType.UpdatedAt);

        return new RoomDetailDto(
            room.Id,
            room.Code,
            room.Floor,
            room.Status,
            roomTypeDto,
            amenities,
            null,
            room.CreatedAt,
            room.UpdatedAt);
    }
}
