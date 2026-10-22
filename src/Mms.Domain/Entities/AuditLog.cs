using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>Nhật ký thay đổi. Được ghi tự động bằng SaveChanges interceptor (làm ở bước sau).</summary>
public class AuditLog : BaseEntity
{
    private AuditLog() { }

    public AuditLog(
        Guid? userId, string entityName, string entityId, string action,
        string? oldValues, string? newValues, DateTimeOffset now)
    {
        UserId = userId;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        CreatedAt = now;
    }

    public Guid? UserId { get; private set; }
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
