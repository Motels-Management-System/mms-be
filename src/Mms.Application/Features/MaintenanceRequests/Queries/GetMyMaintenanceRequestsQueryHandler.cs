using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MaintenanceRequests.Dtos;

namespace Mms.Application.Features.MaintenanceRequests.Queries;

public sealed class GetMyMaintenanceRequestsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetMyMaintenanceRequestsQuery> validator)
    : IQueryHandler<GetMyMaintenanceRequestsQuery, PagedResult<MaintenanceRequestDto>>
{
    public async Task<PagedResult<MaintenanceRequestDto>> HandleAsync(
        GetMyMaintenanceRequestsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var tenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.UserId == query.CurrentUserId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        List<Guid> myRoomIds = tenantId is null
            ? []
            : await dbContext.Contracts
                .AsNoTracking()
                .Where(c => c.Tenants.Any(t => t.TenantId == tenantId.Value))
                .Select(c => c.RoomId)
                .Distinct()
                .ToListAsync(ct);

        var baseQuery = dbContext.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.CreatedByUserId == query.CurrentUserId || myRoomIds.Contains(r.RoomId));

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
