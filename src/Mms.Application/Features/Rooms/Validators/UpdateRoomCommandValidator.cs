using FluentValidation;
using Mms.Application.Features.Rooms.Commands;

namespace Mms.Application.Features.Rooms.Validators;

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã phòng không được để trống.")
            .MaximumLength(20).WithMessage("Mã phòng tối đa 20 ký tự.");

        RuleFor(x => x.RoomTypeId)
            .NotEmpty().WithMessage("Loại phòng không được để trống.");
    }
}
