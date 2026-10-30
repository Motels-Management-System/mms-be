using FluentValidation;
using Mms.Application.Features.Auth.Queries;

namespace Mms.Application.Features.Auth.Validators;

public sealed class GetMeQueryValidator : AbstractValidator<GetMeQuery>
{
    public GetMeQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId không được để trống.");
    }
}
