using FluentValidation;
using Mms.Application.Features.Tenants.Queries;

namespace Mms.Application.Features.Tenants.Validators;

public sealed class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã định danh không được để trống.");
    }
}
