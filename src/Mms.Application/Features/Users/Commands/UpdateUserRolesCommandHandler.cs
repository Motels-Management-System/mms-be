using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;

namespace Mms.Application.Features.Users.Commands;

public sealed class UpdateUserRolesCommandHandler(
    IIdentityService identityService,
    IValidator<UpdateUserRolesCommand> validator) : ICommandHandler<UpdateUserRolesCommand>
{
    public async Task HandleAsync(UpdateUserRolesCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var (succeeded, errors) = await identityService.UpdateUserRolesAsync(
            command.UserId, command.Roles.ToList());

        if (!succeeded)
            throw new ConflictException(string.Join("; ", errors));
    }
}
