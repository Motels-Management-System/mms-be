using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Queries;

public sealed record GetContractsQuery(
    ContractStatus? Status = null,
    Guid? RoomId = null,
    int? ExpiringWithinDays = null,
    int Page = 1,
    int PageSize = 20);
