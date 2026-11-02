namespace Mms.Application.Features.Tenants.Queries;

public sealed record GetTenantContractsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20);
