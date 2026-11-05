using FluentValidation;
using Mms.Application.Features.Contracts.Queries;

namespace Mms.Application.Features.Contracts.Validators;

public sealed class GetContractByIdQueryValidator : AbstractValidator<GetContractByIdQuery>
{
    public GetContractByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id hợp đồng là bắt buộc.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("Không xác định được người dùng hiện tại.");
    }
}
