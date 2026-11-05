using FluentValidation;
using Mms.Application.Features.Reservations.Commands;

namespace Mms.Application.Features.Reservations.Validators;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Phòng là bắt buộc.");

        RuleFor(x => x.TenantId)
            .NotEmpty().When(x => x.TenantId.HasValue)
            .WithMessage("Người thuê không hợp lệ.");
    }
}
