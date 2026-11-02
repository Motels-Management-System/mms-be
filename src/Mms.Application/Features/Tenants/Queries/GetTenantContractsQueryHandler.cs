using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Tenants.Dtos;
using Mms.Domain.ValueObjects;

namespace Mms.Application.Features.Tenants.Queries;

public sealed class GetTenantContractsQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetTenantContractsQuery> validator) : IQueryHandler<GetTenantContractsQuery, PagedResult<TenantContractHistoryDto>>
{
    public async Task<PagedResult<TenantContractHistoryDto>> HandleAsync(GetTenantContractsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var tenantExists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == query.TenantId, ct);

        if (!tenantExists)
            throw new NotFoundException("Hồ sơ người thuê", query.TenantId);

        var historyQuery = from contract in dbContext.Contracts.AsNoTracking()
                           join room in dbContext.Rooms.AsNoTracking() on contract.RoomId equals room.Id
                           join roomType in dbContext.RoomTypes.AsNoTracking() on room.RoomTypeId equals roomType.Id
                           join contractTenant in dbContext.ContractTenants.AsNoTracking()
                               on contract.Id equals contractTenant.ContractId
                           where contractTenant.TenantId == query.TenantId
                           orderby contract.StartDate descending, contract.Id descending
                           select new
                           {
                               ContractId = contract.Id,
                               contract.StartDate,
                               contract.EndDate,
                               contract.Status,
                               RoomId = room.Id,
                               RoomCode = room.Code,
                               room.Floor,
                               RoomTypeName = roomType.Name,
                               contractTenant.IsPrimary,
                           };

        var totalCount = await historyQuery.CountAsync(ct);

        var rows = await historyQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var today = clock.Today;

        var items = rows
            .Select(r => new TenantContractHistoryDto(
                r.ContractId,
                r.RoomId,
                r.RoomCode,
                r.Floor,
                r.RoomTypeName,
                r.StartDate,
                r.EndDate,
                r.Status,
                r.IsPrimary,
                new DateRange(r.StartDate, r.EndDate).DaysUntilEnd(today)))
            .ToList();

        return new PagedResult<TenantContractHistoryDto>(items, query.Page, query.PageSize, totalCount);
    }
}
