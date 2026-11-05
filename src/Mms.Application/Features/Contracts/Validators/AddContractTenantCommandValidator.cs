using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class AddContractTenantCommandValidator : AbstractValidator<AddContractTenantCommand>
{
    public AddContractTenantCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Người thuê là bắt buộc.");
    }
}
