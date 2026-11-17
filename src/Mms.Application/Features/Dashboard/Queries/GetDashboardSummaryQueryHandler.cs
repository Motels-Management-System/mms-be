using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Dashboard.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Dashboard.Queries;

public sealed class GetDashboardSummaryQueryHandler(
    IAppDbContext dbContext,
    IValidator<GetDashboardSummaryQuery> validator) : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> HandleAsync(GetDashboardSummaryQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var countsByStatus = await dbContext.Rooms
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalRooms = countsByStatus.Sum(c => c.Count);

        var byStatus = Enum.GetValues<RoomStatus>()
            .Select(status =>
            {
                var count = countsByStatus.FirstOrDefault(c => c.Status == status)?.Count ?? 0;
                return new RoomStatusCountDto(status, count, Ratio.Of(count, totalRooms));
            })
            .ToList();

        return new DashboardSummaryDto(totalRooms, byStatus);
    }
}
