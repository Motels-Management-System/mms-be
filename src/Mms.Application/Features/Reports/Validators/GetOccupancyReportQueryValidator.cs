using FluentValidation;
using Mms.Application.Features.Reports.Queries;

namespace Mms.Application.Features.Reports.Validators;

public sealed class GetOccupancyReportQueryValidator : AbstractValidator<GetOccupancyReportQuery>
{
    public GetOccupancyReportQueryValidator()
    {
        this.ValidatePeriod();

        RuleFor(x => x.GroupBy)
            .IsInEnum().WithMessage("groupBy chỉ nhận giá trị month hoặc roomType.");
    }
}
