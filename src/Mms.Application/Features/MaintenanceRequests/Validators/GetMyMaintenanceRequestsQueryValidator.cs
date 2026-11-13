using FluentValidation;
using Mms.Application.Features.MaintenanceRequests.Queries;

namespace Mms.Application.Features.MaintenanceRequests.Validators;

public sealed class GetMyMaintenanceRequestsQueryValidator : AbstractValidator<GetMyMaintenanceRequestsQuery>
{
    public GetMyMaintenanceRequestsQueryValidator()
    {
        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
