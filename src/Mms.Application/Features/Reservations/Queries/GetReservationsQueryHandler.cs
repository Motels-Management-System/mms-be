using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reservations.Dtos;

namespace Mms.Application.Features.Reservations.Queries;

public sealed class GetReservationsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetReservationsQuery> validator) : IQueryHandler<GetReservationsQuery, PagedResult<ReservationDto>>
{
    public async Task<PagedResult<ReservationDto>> HandleAsync(GetReservationsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.Reservations.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
            baseQuery = baseQuery.Where(r => r.Status == query.Status.Value);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id,
                r.RoomId,
                RoomCode = dbContext.Rooms.Where(rm => rm.Id == r.RoomId).Select(rm => rm.Code).First(),
                r.TenantId,
                TenantFullName = dbContext.Tenants.Where(t => t.Id == r.TenantId).Select(t => t.FullName).First(),
                TenantPhone = dbContext.Tenants.Where(t => t.Id == r.TenantId).Select(t => t.Phone).First(),
                r.Status,
                r.ExpiresAt,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<ReservationDto>(
            items.Select(r => new ReservationDto(
                r.Id, r.RoomId, r.RoomCode, r.TenantId, r.TenantFullName, r.TenantPhone,
                r.Status, r.ExpiresAt, r.CreatedAt, r.UpdatedAt)).ToList(),
            query.Page,
            query.PageSize,
            totalCount);
    }
}
