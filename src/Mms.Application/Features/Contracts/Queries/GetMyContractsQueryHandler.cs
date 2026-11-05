using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Queries;

public sealed class GetMyContractsQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetMyContractsQuery> validator) : IQueryHandler<GetMyContractsQuery, PagedResult<ContractSummaryDto>>
{
    public async Task<PagedResult<ContractSummaryDto>> HandleAsync(GetMyContractsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var tenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.UserId == query.CurrentUserId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        if (tenantId is null)
            return new PagedResult<ContractSummaryDto>([], query.Page, query.PageSize, 0);

        var baseQuery = dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Tenants.Any(t => t.TenantId == tenantId.Value));

        var totalCount = await baseQuery.CountAsync(ct);

        var today = clock.Today;
        var rows = await baseQuery
            .OrderByDescending(c => c.StartDate)
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
            r.EndDate.DayNumber - today.DayNumber,
            Contract.ExpiryLevelOf(r.Status, today, r.EndDate),
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return new PagedResult<ContractSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }
}
