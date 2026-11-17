using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reports.Dtos;

namespace Mms.Application.Features.Reports.Queries;

public sealed class GetRenewalRateReportQueryHandler(
    IReportQueries reports,
    IValidator<GetRenewalRateReportQuery> validator)
    : IQueryHandler<GetRenewalRateReportQuery, RenewalRateReportDto>
{
    public async Task<RenewalRateReportDto> HandleAsync(GetRenewalRateReportQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);

        var row = await reports.GetRenewalRateAsync(query.From, query.To, ct);

        var turnover = row.ReplacedByNewTenants + row.LeftVacant;

        return new RenewalRateReportDto(
            query.From,
            query.To,
            row.EndedContracts,
            row.RenewedContracts,
            Ratio.Of(row.RenewedContracts, row.EndedContracts),
            turnover,
            Ratio.Of(turnover, row.EndedContracts),
            row.ReplacedByNewTenants,
            row.LeftVacant,
            row.EarlyTerminated);
    }
}
