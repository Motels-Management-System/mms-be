using Mms.Domain.Enums;

namespace Mms.Application.Features.Tenants.Dtos;

public sealed record TenantContractHistoryDto(
    Guid ContractId,
    Guid RoomId,
    string RoomCode,
    int RoomFloor,
    string RoomTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    ContractStatus Status,
    bool IsPrimary,
    int DaysRemaining);
