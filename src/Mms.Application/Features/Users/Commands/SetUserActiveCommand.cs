namespace Mms.Application.Features.Users.Commands;

public sealed record SetUserActiveCommand(
    Guid UserId,
    bool IsActive);
