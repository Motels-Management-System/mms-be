namespace Mms.Application.Features.Users.Commands;

public sealed record UpdateUserRolesCommand(
    Guid UserId,
    IReadOnlyList<string> Roles);
