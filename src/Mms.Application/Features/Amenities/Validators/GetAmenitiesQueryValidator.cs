using FluentValidation;
using Mms.Application.Features.Amenities.Queries;

namespace Mms.Application.Features.Amenities.Validators;

public sealed class GetAmenitiesQueryValidator : AbstractValidator<GetAmenitiesQuery>
{
    public GetAmenitiesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
