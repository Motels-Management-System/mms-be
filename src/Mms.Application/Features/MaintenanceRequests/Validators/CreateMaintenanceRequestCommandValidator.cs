using FluentValidation;
using Mms.Application.Features.MaintenanceRequests.Commands;

namespace Mms.Application.Features.MaintenanceRequests.Validators;

public sealed class CreateMaintenanceRequestCommandValidator
    : AbstractValidator<CreateMaintenanceRequestCommand>
{
    public CreateMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Phòng là bắt buộc.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả sự cố là bắt buộc.")
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");
    }
}
