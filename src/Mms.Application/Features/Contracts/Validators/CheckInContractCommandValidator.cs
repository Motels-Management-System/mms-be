using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class CheckInContractCommandValidator : AbstractValidator<CheckInContractCommand>
{
    public CheckInContractCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.StaffUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");

        RuleFor(x => x.RoomCondition)
            .NotEmpty().WithMessage("Tình trạng phòng là bắt buộc.")
            .MaximumLength(500).WithMessage("Tình trạng phòng tối đa 500 ký tự.");

        RuleFor(x => x.KeyCount)
            .GreaterThanOrEqualTo(0).WithMessage("Số chìa khóa không được âm.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Ghi chú tối đa 2000 ký tự.");
    }
}
