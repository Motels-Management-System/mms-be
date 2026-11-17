using FluentValidation;
using Mms.Application.Features.Reports.Queries;

namespace Mms.Application.Features.Reports.Validators;

public sealed class GetVacancyDurationReportQueryValidator : AbstractValidator<GetVacancyDurationReportQuery>
{
    public GetVacancyDurationReportQueryValidator() => this.ValidatePeriod();
}
