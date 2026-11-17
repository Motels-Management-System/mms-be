using Mms.Domain.Enums;

namespace Mms.Application.Features.Dashboard.Dtos;

/// <summary>Một ô trong lưới phòng: trạng thái phòng + hợp đồng và người thuê hiện tại.</summary>
public sealed record DashboardRoomItemDto(
    Guid RoomId,
    string Code,
    int Floor,
    RoomStatus Status,
    Guid RoomTypeId,
    string RoomTypeName,
    Guid? ContractId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? PrimaryTenantName,
    int? DaysRemaining,
    ExpiryLevel? ExpiryLevel);
