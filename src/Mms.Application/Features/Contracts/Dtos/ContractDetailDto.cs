using Mms.Application.Features.Rooms.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Dtos;

public sealed record ContractDetailDto(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    DateOnly StartDate,
    DateOnly EndDate,
    ContractStatus Status,
    Guid? ReservationId,
    Guid? RenewedFromContractId,
    int DaysRemaining,
    ExpiryLevel ExpiryLevel,
    IReadOnlyList<ContractTenantSummaryDto> Tenants,
    IReadOnlyList<HandoverRecordDto> HandoverRecords,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
