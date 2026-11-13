using FluentValidation;
using Mms.Application.Features.MeterReadings.Queries;

namespace Mms.Application.Features.MeterReadings.Validators;

public sealed class GetMissingMeterReadingsQueryValidator : AbstractValidator<GetMissingMeterReadingsQuery>
{
    public GetMissingMeterReadingsQueryValidator()
    {
        RuleFor(x => x.Month)
            .NotEmpty().WithMessage("Tháng là bắt buộc, định dạng yyyy-MM.")
            .Matches(MeterReadingPeriodRules.MonthPattern).WithMessage("Tháng phải theo định dạng yyyy-MM, ví dụ 2026-11.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
