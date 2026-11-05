using FluentValidation;
using Mms.Application.Features.Contracts.Queries;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class GetContractsQueryValidator : AbstractValidator<GetContractsQuery>
{
    public GetContractsQueryValidator()
    {
        RuleFor(x => x.ExpiringWithinDays)
            .GreaterThanOrEqualTo(1).When(x => x.ExpiringWithinDays.HasValue)
            .WithMessage("Số ngày sắp hết hạn phải lớn hơn 0.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
