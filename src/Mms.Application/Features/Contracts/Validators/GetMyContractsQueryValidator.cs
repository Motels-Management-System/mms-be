using FluentValidation;
using Mms.Application.Features.Contracts.Queries;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class GetMyContractsQueryValidator : AbstractValidator<GetMyContractsQuery>
{
    public GetMyContractsQueryValidator()
    {
        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
