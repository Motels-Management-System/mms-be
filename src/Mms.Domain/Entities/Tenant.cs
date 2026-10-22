using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>
/// Người thuê/người ở trong nghiệp vụ. Không bắt buộc có tài khoản:
/// người ở ghép hoặc khách đến trực tiếp có UserId = null.
/// </summary>
public class Tenant : AuditableEntity
{
    private Tenant() { }

    public Tenant(string fullName, string phone, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        FullName = fullName.Trim();
        Phone = phone.Trim();
        UserId = userId;
    }

    public string FullName { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public Guid? UserId { get; private set; }

    public void UpdateProfile(string fullName, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        FullName = fullName.Trim();
        Phone = phone.Trim();
    }

    public void LinkUser(Guid userId) => UserId = userId;
}
