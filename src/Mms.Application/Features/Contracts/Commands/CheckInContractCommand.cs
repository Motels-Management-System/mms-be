namespace Mms.Application.Features.Contracts.Commands;

/// <summary>Nhận phòng: hợp đồng Draft → Active, phòng → Occupied, ghi biên bản bàn giao.</summary>
public sealed record CheckInContractCommand(
    Guid ContractId,
    Guid StaffUserId,
    string RoomCondition,
    int KeyCount,
    string? Notes = null);
