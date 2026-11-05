using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Queries;

public sealed class GetAvailableRoomsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetAvailableRoomsQuery> validator) : IQueryHandler<GetAvailableRoomsQuery, PagedResult<RoomDto>>
{
    public async Task<PagedResult<RoomDto>> HandleAsync(GetAvailableRoomsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        // Khoảng tìm là nửa mở [From, To), khớp với DateRange của hợp đồng.
        var amenityIds = query.AmenityIds?.Distinct().ToList() ?? [];

        var baseQuery = dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Status != RoomStatus.Maintenance)
            .Where(r => r.RoomType.MaxOccupancy >= query.Guests)
            // Không hợp đồng nào chiếm [From, To): chồng lấn nửa mở Start < To && From < End.
            .Where(r => !dbContext.Contracts.Any(c =>
                c.RoomId == r.Id && c.StartDate < query.To && query.From < c.EndDate))
            // Không giữ phòng còn hiệu lực (unique index UX_Reservations_ActiveHold_Room đảm bảo tối đa 1/phòng).
            .Where(r => !dbContext.Reservations.Any(res =>
                res.RoomId == r.Id &&
                (res.Status == ReservationStatus.Pending || res.Status == ReservationStatus.Confirmed)));

        if (query.RoomTypeId.HasValue)
            baseQuery = baseQuery.Where(r => r.RoomTypeId == query.RoomTypeId.Value);

        if (amenityIds.Count > 0)
        {
            var requiredCount = amenityIds.Count;
            baseQuery = baseQuery.Where(r =>
                dbContext.RoomAmenities.Count(ra => ra.RoomId == r.Id && amenityIds.Contains(ra.AmenityId)) == requiredCount);
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
