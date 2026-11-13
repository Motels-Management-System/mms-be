using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MeterReadings.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.MeterReadings.Queries;

public sealed class GetMissingMeterReadingsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetMissingMeterReadingsQuery> validator)
    : IQueryHandler<GetMissingMeterReadingsQuery, PagedResult<MissingMeterReadingDto>>
{
    public async Task<PagedResult<MissingMeterReadingDto>> HandleAsync(
        GetMissingMeterReadingsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var monthStart = MeterReadingPeriodRules.ParseMonth(query.Month);
        var nextMonth = monthStart.AddMonths(1);

        // "Có người ở" = có hợp đồng không còn là Draft chiếm khoảng [StartDate, EndDate) giao với tháng cần đọc.
        var baseQuery = dbContext.Rooms
            .AsNoTracking()
            .Where(r => dbContext.Contracts.Any(c => c.RoomId == r.Id
                        && c.Status != ContractStatus.Draft
                        && c.StartDate < nextMonth
                        && c.EndDate > monthStart)
                    && !dbContext.MeterReadings.Any(m => m.RoomId == r.Id && m.PeriodMonth == monthStart));

        var totalCount = await baseQuery.CountAsync(ct);

        var rooms = await baseQuery
            .OrderBy(r => r.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new { r.Id, r.Code })
            .ToListAsync(ct);

        if (rooms.Count == 0)
            return new PagedResult<MissingMeterReadingDto>([], query.Page, query.PageSize, totalCount);

        var roomIds = rooms.Select(r => r.Id).ToList();

        var coveringContracts = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => roomIds.Contains(c.RoomId)
                        && c.Status != ContractStatus.Draft
                        && c.StartDate < nextMonth
                        && c.EndDate > monthStart)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new { c.Id, c.RoomId, c.StartDate, c.EndDate })
            .ToListAsync(ct);

        var latestContractByRoom = coveringContracts
            .GroupBy(c => c.RoomId)
            .ToDictionary(g => g.Key, g => g.First());

        var contractIds = latestContractByRoom.Values.Select(c => c.Id).ToList();

        var primaryNames = await dbContext.ContractTenants
            .AsNoTracking()
            .Where(t => contractIds.Contains(t.ContractId) && t.IsPrimary)
            .Join(dbContext.Tenants, t => t.TenantId, t => t.Id, (t, tenant) => new { t.ContractId, tenant.FullName })
            .ToListAsync(ct);

        var primaryNameByContract = primaryNames
            .GroupBy(n => n.ContractId)
            .ToDictionary(g => g.Key, g => (string?)g.First().FullName);

        var items = rooms
            .Where(r => latestContractByRoom.ContainsKey(r.Id))
            .Select(r =>
            {
                var contract = latestContractByRoom[r.Id];
                return new MissingMeterReadingDto(
                    r.Id,
                    r.Code,
                    monthStart,
                    contract.Id,
                    contract.StartDate,
                    contract.EndDate,
                    primaryNameByContract.GetValueOrDefault(contract.Id));
            })
            .ToList();

        return new PagedResult<MissingMeterReadingDto>(items, query.Page, query.PageSize, totalCount);
    }
}
