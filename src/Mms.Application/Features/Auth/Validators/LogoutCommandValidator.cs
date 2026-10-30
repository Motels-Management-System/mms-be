using FluentValidation;
using Mms.Application.Features.Auth.Commands;

namespace Mms.Application.Features.Auth.Validators;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId không được để trống.");
    }
}
