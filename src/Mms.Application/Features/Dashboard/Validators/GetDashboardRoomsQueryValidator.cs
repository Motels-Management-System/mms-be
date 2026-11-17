using FluentValidation;
using Mms.Application.Features.Dashboard.Queries;

namespace Mms.Application.Features.Dashboard.Validators;

public sealed class GetDashboardRoomsQueryValidator : AbstractValidator<GetDashboardRoomsQuery>
{
    public GetDashboardRoomsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
