using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reports.Dtos;

namespace Mms.Application.Features.Reports.Queries;

public sealed class GetOccupancyReportQueryHandler(
    IReportQueries reports,
    IValidator<GetOccupancyReportQuery> validator) : IQueryHandler<GetOccupancyReportQuery, OccupancyReportDto>
{
    public async Task<OccupancyReportDto> HandleAsync(GetOccupancyReportQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var rows = await reports.GetOccupancyAsync(
            query.From, query.To, query.GroupBy == OccupancyGroupBy.RoomType, ct);

        var buckets = rows
            .Select(r => new OccupancyBucketDto(
                r.GroupKey,
                r.PeriodStart,
                r.PeriodEnd,
                r.RoomCount,
                r.RoomDays,
                r.OccupiedRoomDays,
                Ratio.Of(r.OccupiedRoomDays, r.RoomDays)))
            .ToList();

        var overall = Ratio.Of(
            buckets.Sum(b => b.OccupiedRoomDays),
            buckets.Sum(b => b.RoomDays));

        return new OccupancyReportDto(query.From, query.To, query.GroupBy, overall, buckets);
    }
}
