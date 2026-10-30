namespace Mms.Application.Features.Auth.Commands;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword);
