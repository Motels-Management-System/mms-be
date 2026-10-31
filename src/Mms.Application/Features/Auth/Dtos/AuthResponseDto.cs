namespace Mms.Application.Features.Auth.Dtos;

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles);
