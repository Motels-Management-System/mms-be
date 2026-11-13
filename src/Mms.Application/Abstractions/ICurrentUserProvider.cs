namespace Mms.Application.Abstractions;

/// <summary>
/// Người dùng của request đang được xử lý, dùng cho ghi nhật ký kiểm toán.
/// Trả về null khi không có HTTP context (background job, seed).
/// </summary>
public interface ICurrentUserProvider
{
    Guid? UserId { get; }
}
