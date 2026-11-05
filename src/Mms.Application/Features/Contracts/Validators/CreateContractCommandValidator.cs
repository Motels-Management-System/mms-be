using FluentValidation;
using Mms.Application.Features.Contracts.Commands;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Phòng là bắt buộc.");

        RuleFor(x => x.Months)
            .GreaterThanOrEqualTo(1).WithMessage("Thời hạn thuê tính bằng tháng phải lớn hơn 0.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Ngày bắt đầu là bắt buộc.");

        RuleFor(x => x.Tenants)
            .NotEmpty().WithMessage("Hợp đồng cần ít nhất một người ở.");

        RuleForEach(x => x.Tenants)
            .ChildRules(t =>
            {
                t.RuleFor(x => x.TenantId)
                    .NotEmpty().WithMessage("Người thuê không hợp lệ.");
            });
    }
}
