namespace Mms.Application.Features.Users.Commands;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string Role = "Staff");
