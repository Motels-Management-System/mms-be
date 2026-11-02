namespace Mms.Application.Features.Tenants.Dtos;

public sealed record TenantDto(
    Guid Id,
    string FullName,
    string Phone,
    Guid? UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
