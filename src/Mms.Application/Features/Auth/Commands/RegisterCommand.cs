namespace Mms.Application.Features.Auth.Commands;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string Phone);
