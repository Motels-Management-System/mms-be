using FluentValidation;
using Mms.Application.Features.Rooms.Commands;

namespace Mms.Application.Features.Rooms.Validators;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã phòng không được để trống.")
            .MaximumLength(20).WithMessage("Mã phòng tối đa 20 ký tự.");

        RuleFor(x => x.RoomTypeId)
            .NotEmpty().WithMessage("Loại phòng không được để trống.");
    }
}
