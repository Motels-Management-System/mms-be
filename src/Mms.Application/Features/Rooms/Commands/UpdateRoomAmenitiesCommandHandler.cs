using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Application.Features.RoomTypes.Dtos;

namespace Mms.Application.Features.Rooms.Commands;

public sealed class UpdateRoomAmenitiesCommandHandler(
    IAppDbContext dbContext,
    IValidator<UpdateRoomAmenitiesCommand> validator) : ICommandHandler<UpdateRoomAmenitiesCommand, RoomDetailDto>
{
    public async Task<RoomDetailDto> HandleAsync(UpdateRoomAmenitiesCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var room = await dbContext.Rooms
            .Include(r => r.Amenities)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, ct);

        if (room is null)
            throw new NotFoundException("Phòng", command.RoomId);

        var distinctIds = command.AmenityIds.Distinct().ToList();
        List<AmenityDto> amenities = [];

        if (distinctIds.Count > 0)
        {
            var foundAmenities = await dbContext.Amenities
                .AsNoTracking()
                .Where(a => distinctIds.Contains(a.Id))
                .Select(a => new AmenityDto(a.Id, a.Name, a.CreatedAt, a.UpdatedAt))
                .ToListAsync(ct);

            if (foundAmenities.Count != distinctIds.Count)
                throw new NotFoundException("Một hoặc nhiều tiện nghi không tồn tại.");

            amenities = foundAmenities.OrderBy(a => a.Name).ToList();
        }

        room.UpdateAmenities(distinctIds);
        await dbContext.SaveChangesAsync(ct);

        var roomTypeDto = new RoomTypeDto(
            room.RoomType.Id,
            room.RoomType.Name,
            room.RoomType.AreaM2,
            room.RoomType.MaxOccupancy,
            room.RoomType.ListPrice,
            room.RoomType.Description,
            room.RoomType.CreatedAt,
            room.RoomType.UpdatedAt);

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
