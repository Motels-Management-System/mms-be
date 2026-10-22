using Mms.Domain.Common;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Domain.Entities;

/// <summary>Giữ phòng có thời hạn (không cọc). Hết hạn mà chưa thành hợp đồng thì tự chuyển Expired.</summary>
public class Reservation : AuditableEntity
{
    private Reservation() { }

    public static Reservation Create(
        Guid roomId, Guid tenantId, Guid createdByUserId, DateTimeOffset now, TimeSpan holdDuration)
    {
        if (holdDuration <= TimeSpan.Zero)
            throw new DomainException("Thời gian giữ phòng phải lớn hơn 0.");

        return new Reservation
        {
            RoomId = roomId,
            TenantId = tenantId,
            CreatedByUserId = createdByUserId,
            ExpiresAt = now + holdDuration,
            Status = ReservationStatus.Pending
        };
    }

    public Guid RoomId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public ReservationStatus Status { get; private set; }
    public uint Version { get; private set; }

    public bool IsActive => Status is ReservationStatus.Pending or ReservationStatus.Confirmed;

    public void Confirm()
    {
        if (Status != ReservationStatus.Pending)
            throw new DomainException("Chỉ xác nhận được giữ phòng đang ở trạng thái Pending.");
        Status = ReservationStatus.Confirmed;
    }

    public void Cancel()
    {
        if (!IsActive)
            throw new DomainException("Giữ phòng này không còn hiệu lực để hủy.");
        Status = ReservationStatus.Cancelled;
    }

    /// <summary>Dùng cho background job. Trả về true nếu vừa chuyển sang Expired.</summary>
    public bool TryExpire(DateTimeOffset now)
    {
        if (!IsActive || now < ExpiresAt) return false;
        Status = ReservationStatus.Expired;
        return true;
    }

    public void MarkConverted()
    {
        if (Status != ReservationStatus.Confirmed)
            throw new DomainException("Chỉ chuyển thành hợp đồng khi giữ phòng đã được xác nhận.");
        Status = ReservationStatus.ConvertedToContract;
    }
}
