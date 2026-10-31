using FluentValidation;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Auth.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.Auth.Commands;

public sealed class LoginCommandHandler(
    IAppDbContext dbContext,
    IIdentityService identityService,
    IJwtService jwtService,
    IDateTimeProvider clock,
    IValidator<LoginCommand> validator) : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> HandleAsync(LoginCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var (succeeded, userId, email, roles, errors) = await identityService.AuthenticateAsync(
            command.Email.Trim(), command.Password);

        if (!succeeded)
        {
            var errorMessage = errors.Length > 0 ? errors[0] : "Email hoặc mật khẩu không chính xác.";
            throw new UnauthorizedException(errorMessage);
        }

        var (accessToken, accessExpiresAt) = jwtService.GenerateAccessToken(userId, email, roles);
        var (refreshToken, refreshExpiresAt) = jwtService.GenerateRefreshToken();

        var tokenEntity = new RefreshToken(userId, refreshToken, refreshExpiresAt, clock.UtcNow);
        dbContext.RefreshTokens.Add(tokenEntity);

        await dbContext.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            accessExpiresAt,
            userId,
            email,
            roles.ToList());
    }
}
