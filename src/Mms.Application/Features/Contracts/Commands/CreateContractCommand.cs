namespace Mms.Application.Features.Contracts.Commands;

/// <summary>
/// Tạo hợp đồng thuê. ReservationId tùy chọn: nếu có thì giữ phòng (đã xác nhận)
/// được chuyển sang ConvertedToContract trong cùng lần SaveChanges.
/// </summary>
public sealed record CreateContractCommand(
    Guid RoomId,
    DateOnly StartDate,
    int Months,
    IReadOnlyList<ContractTenantItem> Tenants,
    Guid? ReservationId = null);
