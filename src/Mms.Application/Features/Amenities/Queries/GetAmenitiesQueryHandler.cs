using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;

namespace Mms.Application.Features.Amenities.Queries;

public sealed class GetAmenitiesQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetAmenitiesQuery> validator) : IQueryHandler<GetAmenitiesQuery, PagedResult<AmenityDto>>
{
    public async Task<PagedResult<AmenityDto>> HandleAsync(GetAmenitiesQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.Amenities.AsNoTracking();

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(a => a.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AmenityDto(
                a.Id,
                a.Name,
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<AmenityDto>(items, query.Page, query.PageSize, totalCount);
    }
}
