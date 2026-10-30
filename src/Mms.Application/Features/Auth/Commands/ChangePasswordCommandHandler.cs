using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;

namespace Mms.Application.Features.Auth.Commands;

public sealed class ChangePasswordCommandHandler(
    IAppDbContext dbContext,
    IIdentityService identityService,
    IDateTimeProvider clock,
    IValidator<ChangePasswordCommand> validator) : ICommandHandler<ChangePasswordCommand>
{
    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var (succeeded, errors) = await identityService.ChangePasswordAsync(
            command.UserId, command.CurrentPassword, command.NewPassword);

        if (!succeeded)
        {
            throw new ConflictException(string.Join("; ", errors));
        }

        // Revoke all active refresh tokens for security
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == command.UserId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
