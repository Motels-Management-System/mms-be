using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Auth.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler(
    IAppDbContext dbContext,
    IIdentityService identityService,
    IJwtService jwtService,
    IDateTimeProvider clock,
    IValidator<RefreshTokenCommand> validator) : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> HandleAsync(RefreshTokenCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var existingToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == command.RefreshToken, ct);

        if (existingToken is null || !existingToken.IsActive(clock.UtcNow))
        {
            throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        existingToken.Revoke(clock.UtcNow);

        var user = await identityService.GetUserByIdAsync(existingToken.UserId);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Tài khoản người dùng không hợp lệ hoặc đã bị khóa.");
        }

        var (newAccessToken, accessExpiresAt) = jwtService.GenerateAccessToken(user.Id, user.Email, user.Roles.ToList());
        var (newRefreshToken, refreshExpiresAt) = jwtService.GenerateRefreshToken();

        var newTokenEntity = new RefreshToken(user.Id, newRefreshToken, refreshExpiresAt, clock.UtcNow);
        dbContext.RefreshTokens.Add(newTokenEntity);

        await dbContext.SaveChangesAsync(ct);

        return new AuthResponseDto(
            newAccessToken,
            newRefreshToken,
            accessExpiresAt,
            user.Id,
            user.Email,
            user.Roles);
    }
}
