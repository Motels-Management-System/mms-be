using FluentValidation;
using Mms.Application.Features.Tenants.Queries;

namespace Mms.Application.Features.Tenants.Validators;

public sealed class GetTenantContractsQueryValidator : AbstractValidator<GetTenantContractsQuery>
{
    public GetTenantContractsQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
