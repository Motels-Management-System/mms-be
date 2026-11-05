using FluentValidation;
using Mms.Application.Features.Rooms.Queries;

namespace Mms.Application.Features.Rooms.Validators;

public sealed class GetAvailableRoomsQueryValidator : AbstractValidator<GetAvailableRoomsQuery>
{
    public GetAvailableRoomsQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThan(x => x.From).WithMessage("Ngày trả phòng phải sau ngày nhận phòng.");

        RuleFor(x => x.Guests)
            .GreaterThan(0).WithMessage("Số khách phải lớn hơn 0.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");

        RuleFor(x => x.AmenityIds)
            .Must(ids => ids is null || ids.Count <= 20)
            .WithMessage("Chỉ lọc được tối đa 20 tiện nghi mỗi lần.");
    }
}
