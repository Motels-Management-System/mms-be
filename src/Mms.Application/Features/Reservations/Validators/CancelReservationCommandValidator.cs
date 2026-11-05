using FluentValidation;
using Mms.Application.Features.Reservations.Commands;

namespace Mms.Application.Features.Reservations.Validators;

public sealed class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id giữ phòng là bắt buộc.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");
    }
}
