using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Dtos;

public sealed record HandoverRecordDto(
    Guid Id,
    Guid ContractId,
    HandoverType Type,
    string RoomCondition,
    int KeyCount,
    string? Notes,
    DateTimeOffset CreatedAt);
