using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Users.Dtos;

namespace Mms.Application.Features.Users.Commands;

public sealed class CreateUserCommandHandler(
    IIdentityService identityService,
    IValidator<CreateUserCommand> validator) : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var existingUser = await identityService.GetUserByEmailAsync(command.Email.Trim());
        if (existingUser is not null)
            throw new ConflictException("Email này đã được sử dụng.");

        var (succeeded, userId, errors) = await identityService.CreateUserAsync(
            command.Email.Trim(), command.Password, command.Role);

        if (!succeeded)
            throw new ConflictException(string.Join("; ", errors));

        var createdUser = await identityService.GetUserByIdAsync(userId);
        return createdUser!;
    }
}
