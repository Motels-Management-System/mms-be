namespace Mms.Application.Features.Tenants.Commands;

public sealed record UpdateTenantCommand(
    Guid Id,
    string FullName,
    string Phone);
