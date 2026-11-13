using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MaintenanceRequests.Dtos;

namespace Mms.Application.Features.MaintenanceRequests.Queries;

public sealed class GetMaintenanceRequestsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetMaintenanceRequestsQuery> validator)
    : IQueryHandler<GetMaintenanceRequestsQuery, PagedResult<MaintenanceRequestDto>>
{
    public async Task<PagedResult<MaintenanceRequestDto>> HandleAsync(
        GetMaintenanceRequestsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.MaintenanceRequests.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
            baseQuery = baseQuery.Where(r => r.Status == query.Status.Value);

        if (query.RoomId.HasValue)
            baseQuery = baseQuery.Where(r => r.RoomId == query.RoomId.Value);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new MaintenanceRequestDto(
                r.Id,
                r.RoomId,
                dbContext.Rooms.Where(room => room.Id == r.RoomId).Select(room => room.Code).First(),
                r.CreatedByUserId,
                r.Description,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<MaintenanceRequestDto>(items, query.Page, query.PageSize, totalCount);
    }
}
