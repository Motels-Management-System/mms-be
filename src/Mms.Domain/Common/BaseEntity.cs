namespace Mms.Domain.Common;

public abstract class BaseEntity
{
    /// <summary>GUID v7: sinh ở phía ứng dụng, tăng dần theo thời gian nên index hiệu quả.</summary>
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
