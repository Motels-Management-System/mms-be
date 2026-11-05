using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class RenewContractCommandValidator : AbstractValidator<RenewContractCommand>
{
    public RenewContractCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.Months)
            .GreaterThanOrEqualTo(1).WithMessage("Thời hạn gia hạn tính bằng tháng phải lớn hơn 0.");
    }
}
