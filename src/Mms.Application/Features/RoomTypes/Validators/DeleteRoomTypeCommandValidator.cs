using FluentValidation;
using Mms.Application.Features.RoomTypes.Commands;

namespace Mms.Application.Features.RoomTypes.Validators;

public sealed class DeleteRoomTypeCommandValidator : AbstractValidator<DeleteRoomTypeCommand>
{
    public DeleteRoomTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");
    }
}
