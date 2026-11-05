namespace Mms.Application.Features.Contracts.Commands;

/// <summary>Đổi phòng cho hợp đồng đang Active: phòng mới phải trống trong thời gian còn lại.</summary>
public sealed record ChangeContractRoomCommand(
    Guid ContractId,
    Guid NewRoomId);
