using FluentValidation;
using Mms.Application.Features.MaintenanceRequests.Commands;

namespace Mms.Application.Features.MaintenanceRequests.Validators;

public sealed class StartMaintenanceRequestCommandValidator : AbstractValidator<StartMaintenanceRequestCommand>
{
    public StartMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id yêu cầu bảo trì là bắt buộc.");
    }
}
