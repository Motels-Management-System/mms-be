namespace Mms.Application.Features.Auth.Dtos;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    TenantProfileDto? TenantProfile);

public sealed record TenantProfileDto(
    Guid Id,
    string FullName,
    string Phone);
