using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class TerminateContractCommandValidator : AbstractValidator<TerminateContractCommand>
{
    public TerminateContractCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.TerminationDate)
            .NotEmpty().WithMessage("Ngày chấm dứt là bắt buộc.");
    }
}
