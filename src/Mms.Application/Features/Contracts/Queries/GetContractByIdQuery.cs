namespace Mms.Application.Features.Contracts.Queries;

public sealed record GetContractByIdQuery(
    Guid Id,
    Guid CurrentUserId,
    bool IsStaffOrAdmin);
