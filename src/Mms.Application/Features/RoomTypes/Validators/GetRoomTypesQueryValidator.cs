using FluentValidation;
using Mms.Application.Features.RoomTypes.Queries;

namespace Mms.Application.Features.RoomTypes.Validators;

public sealed class GetRoomTypesQueryValidator : AbstractValidator<GetRoomTypesQuery>
{
    public GetRoomTypesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
