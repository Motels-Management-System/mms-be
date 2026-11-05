namespace Mms.Application.Features.Contracts.Queries;

public sealed record GetMyContractsQuery(
    Guid CurrentUserId,
    int Page = 1,
    int PageSize = 20);
