using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Dtos;

/// <summary>Thông tin dòng hợp đồng dùng cho danh sách (luôn kèm daysRemaining/expiryLevel).</summary>
public sealed record ContractSummaryDto(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    DateOnly StartDate,
    DateOnly EndDate,
    ContractStatus Status,
    Guid? RenewedFromContractId,
    string? PrimaryTenantName,
    int DaysRemaining,
    ExpiryLevel ExpiryLevel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
