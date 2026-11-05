using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class RemoveContractTenantCommandValidator : AbstractValidator<RemoveContractTenantCommand>
{
    public RemoveContractTenantCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Người thuê là bắt buộc.");
    }
}
