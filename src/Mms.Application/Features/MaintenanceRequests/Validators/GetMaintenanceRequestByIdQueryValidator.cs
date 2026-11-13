using FluentValidation;
using Mms.Application.Features.MaintenanceRequests.Queries;

namespace Mms.Application.Features.MaintenanceRequests.Validators;

public sealed class GetMaintenanceRequestByIdQueryValidator : AbstractValidator<GetMaintenanceRequestByIdQuery>
{
    public GetMaintenanceRequestByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id yêu cầu bảo trì là bắt buộc.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");
    }
}
