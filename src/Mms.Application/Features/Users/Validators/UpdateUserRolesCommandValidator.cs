using FluentValidation;
using Mms.Application.Features.Users.Commands;

namespace Mms.Application.Features.Users.Validators;

public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    private static readonly string[] AllowedRoles = ["Admin", "Staff", "Customer"];

    public UpdateUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId không được để trống.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("Danh sách vai trò không được để trống.")
            .Must(roles => roles.All(r => AllowedRoles.Contains(r)))
            .WithMessage("Vai trò không hợp lệ. Chỉ chấp nhận Admin, Staff hoặc Customer.");
    }
}
