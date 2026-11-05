namespace Mms.Application.Features.Contracts.Commands;

/// <summary>Trả phòng: hợp đồng đã Expired/Terminated, ghi biên bản, phòng → Available hoặc Maintenance.</summary>
public sealed record CheckOutContractCommand(
    Guid ContractId,
    Guid StaffUserId,
    string RoomCondition,
    int KeyCount,
    string? Notes = null,
    bool ToMaintenance = false);
