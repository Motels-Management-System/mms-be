namespace Mms.Application.Features.Tenants.Commands;

public sealed record CreateTenantCommand(
    string FullName,
    string Phone);
