using FluentValidation;
using Mms.Application.Features.Reservations.Commands;

namespace Mms.Application.Features.Reservations.Validators;

public sealed class ConfirmReservationCommandValidator : AbstractValidator<ConfirmReservationCommand>
{
    public ConfirmReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id giữ phòng là bắt buộc.");
    }
}

