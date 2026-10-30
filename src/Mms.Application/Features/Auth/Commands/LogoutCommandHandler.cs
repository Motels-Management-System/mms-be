using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;

namespace Mms.Application.Features.Auth.Commands;

public sealed class LogoutCommandHandler(
    IAppDbContext dbContext,
    IDateTimeProvider clock,
    IValidator<LogoutCommand> validator) : ICommandHandler<LogoutCommand>
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var query = dbContext.RefreshTokens
            .Where(t => t.UserId == command.UserId && t.RevokedAt == null);

        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            query = query.Where(t => t.Token == command.RefreshToken);
        }

        var tokens = await query.ToListAsync(ct);
        foreach (var token in tokens)
        {
            token.Revoke(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
