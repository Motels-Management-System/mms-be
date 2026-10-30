namespace Mms.Application.Features.Auth.Commands;

public sealed record LogoutCommand(Guid UserId, string? RefreshToken = null);
