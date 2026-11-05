using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

/// <summary>
/// Biên bản bàn giao phòng lúc nhận (CheckIn) và trả (CheckOut):
/// tình trạng phòng, số chìa khóa, ghi chú thêm.
/// </summary>
public class HandoverRecord : AuditableEntity
{
    private HandoverRecord() { }

    public HandoverRecord(
        Guid contractId, HandoverType type, string roomCondition, int keyCount,
        Guid createdByUserId, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCondition);

        if (keyCount < 0)
            throw new ArgumentException("Số chìa khóa không được âm.", nameof(keyCount));

        ContractId = contractId;
        Type = type;
        RoomCondition = roomCondition.Trim();
        KeyCount = keyCount;
        Notes = notes?.Trim();
        CreatedByUserId = createdByUserId;
    }

    public Guid ContractId { get; private set; }
    public HandoverType Type { get; private set; }
    public string RoomCondition { get; private set; } = default!;
    public int KeyCount { get; private set; }
    public string? Notes { get; private set; }
    public Guid CreatedByUserId { get; private set; }
}
