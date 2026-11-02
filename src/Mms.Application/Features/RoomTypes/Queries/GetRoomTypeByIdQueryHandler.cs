using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.RoomTypes.Dtos;

namespace Mms.Application.Features.RoomTypes.Queries;

public sealed class GetRoomTypeByIdQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetRoomTypeByIdQuery> validator) : IQueryHandler<GetRoomTypeByIdQuery, RoomTypeDto>
{
    public async Task<RoomTypeDto> HandleAsync(GetRoomTypeByIdQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var roomType = await dbContext.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.Id == query.Id)
            .Select(rt => new RoomTypeDto(
                rt.Id,
                rt.Name,
                rt.AreaM2,
                rt.MaxOccupancy,
                rt.ListPrice,
                rt.Description,
                rt.CreatedAt,
                rt.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        if (roomType is null)
            throw new NotFoundException("Không tìm thấy loại phòng.");

        return roomType;
    }
}
