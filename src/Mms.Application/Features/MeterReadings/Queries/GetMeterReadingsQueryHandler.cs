using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MeterReadings.Dtos;

namespace Mms.Application.Features.MeterReadings.Queries;

public sealed class GetMeterReadingsQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetMeterReadingsQuery> validator)
    : IQueryHandler<GetMeterReadingsQuery, PagedResult<MeterReadingDto>>
{
    public async Task<PagedResult<MeterReadingDto>> HandleAsync(GetMeterReadingsQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.RoomId, ct)
            ?? throw new NotFoundException("Phòng", query.RoomId);

        var baseQuery = dbContext.MeterReadings
            .AsNoTracking()
            .Where(m => m.RoomId == room.Id);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(m => m.PeriodMonth)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new MeterReadingDto(
                m.Id,
                m.RoomId,
                room.Code,
                m.PeriodMonth,
                m.Electricity,
                m.Water,
                m.IsMeterReplaced,
                m.CreatedAt,
                m.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<MeterReadingDto>(items, query.Page, query.PageSize, totalCount);
    }
}
