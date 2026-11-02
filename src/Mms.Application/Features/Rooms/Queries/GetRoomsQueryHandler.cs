using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;

namespace Mms.Application.Features.Rooms.Queries;

public sealed class GetRoomsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetRoomsQuery> validator) : IQueryHandler<GetRoomsQuery, PagedResult<RoomDto>>
{
    public async Task<PagedResult<RoomDto>> HandleAsync(GetRoomsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.Rooms.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
            baseQuery = baseQuery.Where(r => r.Status == query.Status.Value);

        if (query.RoomTypeId.HasValue)
            baseQuery = baseQuery.Where(r => r.RoomTypeId == query.RoomTypeId.Value);

        if (query.Floor.HasValue)
            baseQuery = baseQuery.Where(r => r.Floor == query.Floor.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToUpper();
            baseQuery = baseQuery.Where(r => r.Code.Contains(kw));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var pagedRooms = await baseQuery
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Floor,
                r.Status,
                r.RoomTypeId,
                RoomTypeName = r.RoomType.Name,
                AreaM2 = r.RoomType.AreaM2,
                MaxOccupancy = r.RoomType.MaxOccupancy,
                ListPrice = r.RoomType.ListPrice,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync(ct);

        var roomIds = pagedRooms.Select(r => r.Id).ToList();

        var roomAmenities = await dbContext.RoomAmenities
            .AsNoTracking()
            .Where(ra => roomIds.Contains(ra.RoomId))
            .Join(
                dbContext.Amenities.AsNoTracking(),
                ra => ra.AmenityId,
                a => a.Id,
                (ra, a) => new { ra.RoomId, Amenity = new AmenityDto(a.Id, a.Name, a.CreatedAt, a.UpdatedAt) })
            .ToListAsync(ct);

        var amenitiesByRoom = roomAmenities
            .GroupBy(x => x.RoomId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AmenityDto>)g.Select(x => x.Amenity).OrderBy(a => a.Name).ToList());

        var items = pagedRooms.Select(r => new RoomDto(
            r.Id,
            r.Code,
            r.Floor,
            r.Status,
            r.RoomTypeId,
            r.RoomTypeName,
            r.AreaM2,
            r.MaxOccupancy,
            r.ListPrice,
            amenitiesByRoom.GetValueOrDefault(r.Id, Array.Empty<AmenityDto>()),
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return new PagedResult<RoomDto>(items, query.Page, query.PageSize, totalCount);
    }
}
