using Mms.Application.Abstractions;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Application.Features.Contracts.Dtos;

/// <summary>Dựng DTO hợp đồng từ entity đã load đủ Tenants/HandoverRecords và mã phòng.</summary>
internal static class ContractDtoMapper
{
    public static ContractDetailDto ToDetail(
        Contract contract,
        string roomCode,
        IDateTimeProvider clock,
        IReadOnlyList<HandoverRecord>? handoverRecords = null,
        IReadOnlyDictionary<Guid, (string FullName, string Phone)>? tenantLookup = null)
    {
        var today = clock.Today;
        var tenants = contract.Tenants
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => tenantLookup?.GetValueOrDefault(t.TenantId).FullName ?? string.Empty)
            .Select(t =>
            {
                var info = tenantLookup?.GetValueOrDefault(t.TenantId) ?? (string.Empty, string.Empty);
                return new ContractTenantSummaryDto(t.TenantId, info.FullName, info.Phone, t.IsPrimary);
            })
            .ToList();

        var handovers = (handoverRecords ?? [])
            .OrderBy(h => h.CreatedAt)
            .Select(h => new HandoverRecordDto(h.Id, h.ContractId, h.Type, h.RoomCondition, h.KeyCount, h.Notes, h.CreatedAt))
            .ToList();

        return new ContractDetailDto(
            contract.Id,
            contract.RoomId,
            roomCode,
            contract.StartDate,
            contract.EndDate,
            contract.Status,
            contract.ReservationId,
            contract.RenewedFromContractId,
            contract.DaysRemaining(today),
            contract.GetExpiryLevel(today),
            tenants,
            handovers,
            contract.CreatedAt,
            contract.UpdatedAt);
    }

    public static ContractSummaryDto ToSummary(
        Contract contract,
        string roomCode,
        string? primaryTenantName,
        IDateTimeProvider clock)
    {
        var today = clock.Today;
        return new ContractSummaryDto(
            contract.Id,
            contract.RoomId,
            roomCode,
            contract.StartDate,
            contract.EndDate,
            contract.Status,
            contract.RenewedFromContractId,
            primaryTenantName,
            contract.DaysRemaining(today),
            contract.GetExpiryLevel(today),
            contract.CreatedAt,
            contract.UpdatedAt);
    }
}
