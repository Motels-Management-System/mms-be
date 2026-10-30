using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Auth.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.Features.Auth.Commands;

public sealed class RegisterCommandHandler(
    IAppDbContext dbContext,
    IIdentityService identityService,
    IJwtService jwtService,
    IDateTimeProvider clock,
    IValidator<RegisterCommand> validator) : ICommandHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> HandleAsync(RegisterCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        // Check if phone is already registered as tenant
        var phoneExists = await dbContext.Tenants.AnyAsync(t => t.Phone == command.Phone.Trim(), ct);
        if (phoneExists)
            throw new ConflictException("Số điện thoại này đã được đăng ký trong hệ thống.");

        // Check if email already used
        var existingUser = await identityService.GetUserByEmailAsync(command.Email.Trim());
        if (existingUser is not null)
            throw new ConflictException("Email này đã được sử dụng.");

        // Create identity user with role Customer
        var (succeeded, userId, errors) = await identityService.CreateUserAsync(
            command.Email.Trim(), command.Password, "Customer");

        if (!succeeded)
            throw new ConflictException(string.Join("; ", errors));

        // Create tenant profile
        var tenant = new Tenant(command.FullName, command.Phone, userId);
        dbContext.Tenants.Add(tenant);

        // Issue tokens
        var roles = new List<string> { "Customer" };
        var (accessToken, accessExpiresAt) = jwtService.GenerateAccessToken(userId, command.Email.Trim(), roles);
        var (refreshToken, refreshExpiresAt) = jwtService.GenerateRefreshToken();

        var tokenEntity = new RefreshToken(userId, refreshToken, refreshExpiresAt, clock.UtcNow);
        dbContext.RefreshTokens.Add(tokenEntity);

        await dbContext.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            accessExpiresAt,
            userId,
            command.Email.Trim(),
            roles);
    }
}
