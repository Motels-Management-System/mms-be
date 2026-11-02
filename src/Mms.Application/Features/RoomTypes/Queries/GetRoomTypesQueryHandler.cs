using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.RoomTypes.Dtos;

namespace Mms.Application.Features.RoomTypes.Queries;

public sealed class GetRoomTypesQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetRoomTypesQuery> validator) : IQueryHandler<GetRoomTypesQuery, PagedResult<RoomTypeDto>>
{
    public async Task<PagedResult<RoomTypeDto>> HandleAsync(GetRoomTypesQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.RoomTypes.AsNoTracking();

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(rt => rt.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(rt => new RoomTypeDto(
                rt.Id,
                rt.Name,
                rt.AreaM2,
                rt.MaxOccupancy,
                rt.ListPrice,
                rt.Description,
                rt.CreatedAt,
                rt.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<RoomTypeDto>(items, query.Page, query.PageSize, totalCount);
    }
}
