namespace Mms.Application.Features.Tenants.Queries;

public sealed record GetTenantsQuery(
    string? Keyword = null,
    int Page = 1,
    int PageSize = 20);
