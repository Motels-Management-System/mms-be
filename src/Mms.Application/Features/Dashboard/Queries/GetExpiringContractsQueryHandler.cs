using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Dashboard.Queries;

public sealed class GetExpiringContractsQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetExpiringContractsQuery> validator)
    : IQueryHandler<GetExpiringContractsQuery, PagedResult<ContractSummaryDto>>
{
    public async Task<PagedResult<ContractSummaryDto>> HandleAsync(
        GetExpiringContractsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var today = clock.Today;
        var until = today.AddDays(query.Days);

        var baseQuery = dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Status == ContractStatus.Active && c.EndDate >= today && c.EndDate <= until);

        var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderBy(c => c.EndDate)
            .ThenBy(c => c.Id)
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
                    .Where(ct2 => ct2.ContractId == c.Id && ct2.IsPrimary)
                    .Join(dbContext.Tenants, ct2 => ct2.TenantId, t => t.Id, (ct2, t) => t.FullName)
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
            r.EndDate.DayNumber - today.DayNumber,
            Contract.ExpiryLevelOf(r.Status, today, r.EndDate),
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return new PagedResult<ContractSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }
}
