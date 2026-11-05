using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class ChangeContractRoomCommandValidator : AbstractValidator<ChangeContractRoomCommand>
{
    public ChangeContractRoomCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.NewRoomId)
            .NotEmpty().WithMessage("Phòng mới là bắt buộc.");
    }
}
