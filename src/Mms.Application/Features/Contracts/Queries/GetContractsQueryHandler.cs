using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Queries;

public sealed class GetContractsQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetContractsQuery> validator) : IQueryHandler<GetContractsQuery, PagedResult<ContractSummaryDto>>
{
    public async Task<PagedResult<ContractSummaryDto>> HandleAsync(GetContractsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var baseQuery = dbContext.Contracts.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
            baseQuery = baseQuery.Where(c => c.Status == query.Status.Value);

        if (query.RoomId.HasValue)
            baseQuery = baseQuery.Where(c => c.RoomId == query.RoomId.Value);

        if (query.ExpiringWithinDays.HasValue)
        {
            var today = clock.Today;
            var until = today.AddDays(query.ExpiringWithinDays.Value);

            // Đang hiệu lực và kết thúc trong khoảng [today, today + days].
            baseQuery = baseQuery.Where(c =>
                c.Status == ContractStatus.Active && c.EndDate >= today && c.EndDate <= until);
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var todayNow = clock.Today;
        var rows = await baseQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                c.Id,
                c.RoomId,
                RoomCode = dbContext.Rooms.Where(r => r.Id == c.RoomId).Select(r => r.Code).First(),
                c.StartDate,
                c.EndDate,
                c.Status,
                c.RenewedFromContractId,
                PrimaryTenantName = dbContext.ContractTenants
                    .Where(ct => ct.ContractId == c.Id && ct.IsPrimary)
                    .Join(dbContext.Tenants, ct => ct.TenantId, t => t.Id, (ct, t) => t.FullName)
                    .FirstOrDefault(),
                c.CreatedAt,
                c.UpdatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ContractSummaryDto(
            r.Id,
            r.RoomId,
            r.RoomCode,
            r.StartDate,
            r.EndDate,
            r.Status,
            r.RenewedFromContractId,
            r.PrimaryTenantName,
            r.EndDate.DayNumber - todayNow.DayNumber,
            Contract.ExpiryLevelOf(r.Status, todayNow, r.EndDate),
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return new PagedResult<ContractSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }
}
