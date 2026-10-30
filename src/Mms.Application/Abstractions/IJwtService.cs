namespace Mms.Application.Abstractions;

public interface IJwtService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Guid userId, string email, IList<string> roles);
    (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken();
}
