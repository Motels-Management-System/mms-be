using FluentValidation;
using Mms.Application.Features.RoomTypes.Commands;

namespace Mms.Application.Features.RoomTypes.Validators;

public sealed class UpdateRoomTypeCommandValidator : AbstractValidator<UpdateRoomTypeCommand>
{
    public UpdateRoomTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên loại phòng không được để trống.")
            .MaximumLength(100).WithMessage("Tên loại phòng tối đa 100 ký tự.");

        RuleFor(x => x.AreaM2)
            .GreaterThan(0).WithMessage("Diện tích phải lớn hơn 0.");

        RuleFor(x => x.MaxOccupancy)
            .GreaterThan(0).WithMessage("Số người ở tối đa phải lớn hơn 0.");

        RuleFor(x => x.ListPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá niêm yết không được âm.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");
    }
}
