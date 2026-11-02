using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Application.Features.RoomTypes.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.Rooms.Commands;

public sealed class CreateRoomCommandHandler(
    IAppDbContext dbContext,
    IValidator<CreateRoomCommand> validator) : ICommandHandler<CreateRoomCommand, RoomDetailDto>
{
    public async Task<RoomDetailDto> HandleAsync(CreateRoomCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        var codeExists = await dbContext.Rooms
            .AnyAsync(r => r.Code == normalizedCode, ct);

        if (codeExists)
            throw new ConflictException("Mã phòng đã tồn tại.");

        var roomType = await dbContext.RoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == command.RoomTypeId, ct);

        if (roomType is null)
            throw new NotFoundException("Loại phòng", command.RoomTypeId);

        var amenityIds = command.AmenityIds?.Distinct().ToList() ?? [];
        List<AmenityDto> amenities = [];

        if (amenityIds.Count > 0)
        {
            var foundAmenities = await dbContext.Amenities
                .AsNoTracking()
                .Where(a => amenityIds.Contains(a.Id))
                .Select(a => new AmenityDto(a.Id, a.Name, a.CreatedAt, a.UpdatedAt))
                .ToListAsync(ct);

            if (foundAmenities.Count != amenityIds.Count)
                throw new NotFoundException("Một hoặc nhiều tiện nghi không tồn tại.");

            amenities = foundAmenities.OrderBy(a => a.Name).ToList();
        }

        var room = new Room(command.Code, command.Floor, command.RoomTypeId);
        foreach (var amenityId in amenityIds)
        {
            room.AddAmenity(amenityId);
        }

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(ct);

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
