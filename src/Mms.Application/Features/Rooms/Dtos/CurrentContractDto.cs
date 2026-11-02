using Mms.Domain.Enums;

namespace Mms.Application.Features.Rooms.Dtos;

public sealed record CurrentContractDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    ContractStatus Status,
    int DaysRemaining,
    IReadOnlyList<ContractTenantSummaryDto> Tenants);

public sealed record ContractTenantSummaryDto(
    Guid TenantId,
    string FullName,
    string Phone,
    bool IsPrimary);
