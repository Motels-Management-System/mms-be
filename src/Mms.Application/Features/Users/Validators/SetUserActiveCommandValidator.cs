using FluentValidation;
using Mms.Application.Features.Users.Commands;

namespace Mms.Application.Features.Users.Validators;

public sealed class SetUserActiveCommandValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId không được để trống.");
    }
}
