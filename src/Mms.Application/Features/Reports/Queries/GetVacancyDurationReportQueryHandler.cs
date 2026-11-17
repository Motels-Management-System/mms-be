using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reports.Dtos;

namespace Mms.Application.Features.Reports.Queries;

public sealed class GetVacancyDurationReportQueryHandler(
    IReportQueries reports,
    IValidator<GetVacancyDurationReportQuery> validator)
    : IQueryHandler<GetVacancyDurationReportQuery, VacancyDurationReportDto>
{
    public async Task<VacancyDurationReportDto> HandleAsync(
        GetVacancyDurationReportQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var rows = await reports.GetVacancyGapsAsync(query.From, query.To, ct);

        var items = rows
            .Select(r => new RoomVacancyDto(
                r.RoomId,
                r.RoomCode,
                r.RoomTypeName,
                r.GapCount,
                r.VacancyDays,
                Ratio.Of(r.VacancyDays, r.GapCount),
                r.MaxVacancyDays))
            .OrderByDescending(i => i.AvgVacancyDays)
            .ThenBy(i => i.RoomCode)
            .ToList();

        var totalGaps = rows.Sum(r => r.GapCount);
        var totalVacancyDays = rows.Sum(r => r.VacancyDays);

        return new VacancyDurationReportDto(
            query.From,
            query.To,
            items.Count,
            totalGaps,
            Ratio.Of(totalVacancyDays, totalGaps),
            items);
    }
}
