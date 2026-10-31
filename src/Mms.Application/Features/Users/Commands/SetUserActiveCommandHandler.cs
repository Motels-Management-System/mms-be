using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;

namespace Mms.Application.Features.Users.Commands;

public sealed class SetUserActiveCommandHandler(
    IIdentityService identityService,
    IValidator<SetUserActiveCommand> validator) : ICommandHandler<SetUserActiveCommand>
{
    public async Task HandleAsync(SetUserActiveCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var (succeeded, errors) = await identityService.SetUserActiveAsync(
            command.UserId, command.IsActive);

        if (!succeeded)
            throw new ConflictException(string.Join("; ", errors));
    }
}
