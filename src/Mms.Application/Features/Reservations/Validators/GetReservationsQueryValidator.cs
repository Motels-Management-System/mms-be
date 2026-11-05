using FluentValidation;
using Mms.Application.Features.Reservations.Queries;

namespace Mms.Application.Features.Reservations.Validators;

public sealed class GetReservationsQueryValidator : AbstractValidator<GetReservationsQuery>
{
    public GetReservationsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
