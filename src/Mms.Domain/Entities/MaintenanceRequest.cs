using Mms.Domain.Common;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Domain.Entities;

public class MaintenanceRequest : AuditableEntity
{
    private MaintenanceRequest() { }

    public MaintenanceRequest(Guid roomId, Guid createdByUserId, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        RoomId = roomId;
        CreatedByUserId = createdByUserId;
        Description = description.Trim();
        Status = MaintenanceStatus.Open;
    }

    public Guid RoomId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Description { get; private set; } = default!;
    public MaintenanceStatus Status { get; private set; }

    public void Start()
    {
        if (Status != MaintenanceStatus.Open)
            throw new DomainException("Chỉ nhận xử lý được yêu cầu đang Open.");
        Status = MaintenanceStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != MaintenanceStatus.InProgress)
            throw new DomainException("Chỉ hoàn tất được yêu cầu đang InProgress.");
        Status = MaintenanceStatus.Done;
    }
}
