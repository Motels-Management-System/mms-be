namespace Mms.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void MarkCreated(DateTimeOffset now) => CreatedAt = now;
    public void MarkUpdated(DateTimeOffset now) => UpdatedAt = now;
}
