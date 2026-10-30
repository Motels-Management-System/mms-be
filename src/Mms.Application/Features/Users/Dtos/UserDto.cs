namespace Mms.Application.Features.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
