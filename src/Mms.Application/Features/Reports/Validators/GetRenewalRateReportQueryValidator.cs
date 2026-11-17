using FluentValidation;
using Mms.Application.Features.Reports.Queries;

namespace Mms.Application.Features.Reports.Validators;

public sealed class GetRenewalRateReportQueryValidator : AbstractValidator<GetRenewalRateReportQuery>
{
    public GetRenewalRateReportQueryValidator() => this.ValidatePeriod();
}
