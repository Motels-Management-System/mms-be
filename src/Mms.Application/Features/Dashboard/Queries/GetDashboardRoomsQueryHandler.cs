using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Dashboard.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Dashboard.Queries;

public sealed class GetDashboardRoomsQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetDashboardRoomsQuery> validator)
    : IQueryHandler<GetDashboardRoomsQuery, PagedResult<DashboardRoomItemDto>>
{
    public async Task<PagedResult<DashboardRoomItemDto>> HandleAsync(
        GetDashboardRoomsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var today = clock.Today;

        var baseQuery = dbContext.Rooms.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(ct);

        // Hợp đồng hiện tại = hợp đồng không còn Draft đang chiếm hôm nay theo khoảng nửa mở [Start, End).
        var rows = await baseQuery
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Floor,
                r.Status,
                r.RoomTypeId,
                RoomTypeName = r.RoomType.Name,
                Contract = dbContext.Contracts
                    .Where(c => c.RoomId == r.Id
                                && c.Status != ContractStatus.Draft
                                && c.StartDate <= today
                                && c.EndDate > today)
                    .OrderByDescending(c => c.StartDate)
                    .Select(c => new { c.Id, c.StartDate, c.EndDate, c.Status })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var contractIds = rows.Where(r => r.Contract is not null).Select(r => r.Contract!.Id).ToList();

        var primaryNames = await dbContext.ContractTenants
            .AsNoTracking()
            .Where(t => contractIds.Contains(t.ContractId) && t.IsPrimary)
            .Join(dbContext.Tenants, t => t.TenantId, t => t.Id, (t, tenant) => new { t.ContractId, tenant.FullName })
            .ToListAsync(ct);

        var primaryNameByContract = primaryNames
            .GroupBy(n => n.ContractId)
            .ToDictionary(g => g.Key, g => (string?)g.First().FullName);

        var items = rows.Select(r =>
        {
            var contract = r.Contract;
            return new DashboardRoomItemDto(
                r.Id,
                r.Code,
                r.Floor,
                r.Status,
                r.RoomTypeId,
                r.RoomTypeName,
                contract?.Id,
                contract?.StartDate,
                contract?.EndDate,
                contract is null ? null : primaryNameByContract.GetValueOrDefault(contract.Id),
                contract is null ? null : contract.EndDate.DayNumber - today.DayNumber,
                contract is null ? null : Contract.ExpiryLevelOf(contract.Status, today, contract.EndDate));
        }).ToList();

        return new PagedResult<DashboardRoomItemDto>(items, query.Page, query.PageSize, totalCount);
    }
}
