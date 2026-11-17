using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reports.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Reports.Queries;

public sealed class GetVacancyForecastQueryHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<GetVacancyForecastQuery> validator)
    : IQueryHandler<GetVacancyForecastQuery, PagedResult<VacancyForecastItemDto>>
{
    public async Task<PagedResult<VacancyForecastItemDto>> HandleAsync(
        GetVacancyForecastQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var today = clock.Today;
        var horizon = today.AddDays(query.Days);

        // Hợp đồng Active kết thúc trong (today, today + days] và không có hợp đồng nào
        // (kể cả Draft) bắt đầu đúng ngày đó để nối tiếp, tức phòng sẽ thật sự trống.
        var baseQuery = dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Status == ContractStatus.Active
                        && c.EndDate > today
                        && c.EndDate <= horizon
                        && !dbContext.Contracts.Any(s =>
                            s.RoomId == c.RoomId && s.Id != c.Id && s.StartDate == c.EndDate));

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
                c.EndDate,
                PrimaryTenantName = dbContext.ContractTenants
                    .Where(t => t.ContractId == c.Id && t.IsPrimary)
                    .Join(dbContext.Tenants, t => t.TenantId, tenant => tenant.Id, (t, tenant) => tenant.FullName)
                    .FirstOrDefault(),
                NextContractStartDate = dbContext.Contracts
                    .Where(s => s.RoomId == c.RoomId && s.Id != c.Id && s.StartDate > c.EndDate)
                    .OrderBy(s => s.StartDate)
                    .Select(s => (DateOnly?)s.StartDate)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var roomIds = rows.Select(r => r.RoomId).ToList();

        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => roomIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Floor,
                r.Status,
                r.RoomTypeId,
                RoomTypeName = r.RoomType.Name
            })
            .ToListAsync(ct);

        var roomById = rooms.ToDictionary(r => r.Id);

        var items = rows
            .Where(r => roomById.ContainsKey(r.RoomId))
            .Select(r =>
            {
                var room = roomById[r.RoomId];
                return new VacancyForecastItemDto(
                    room.Id,
                    room.Code,
                    room.Floor,
                    room.RoomTypeId,
                    room.RoomTypeName,
                    room.Status,
                    r.Id,
                    r.EndDate,
                    r.EndDate.DayNumber - today.DayNumber,
                    r.PrimaryTenantName,
                    r.NextContractStartDate);
            })
            .ToList();

        return new PagedResult<VacancyForecastItemDto>(items, query.Page, query.PageSize, totalCount);
    }
}
