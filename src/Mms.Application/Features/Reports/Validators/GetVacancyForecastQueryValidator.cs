using FluentValidation;
using Mms.Application.Features.Reports.Queries;

namespace Mms.Application.Features.Reports.Validators;

public sealed class GetVacancyForecastQueryValidator : AbstractValidator<GetVacancyForecastQuery>
{
    public GetVacancyForecastQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 365).WithMessage("Số ngày dự báo phải từ 1 đến 365.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
