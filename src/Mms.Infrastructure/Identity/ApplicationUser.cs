using Microsoft.AspNetCore.Identity;

namespace Mms.Infrastructure.Identity;

/// <summary>Tài khoản đăng nhập. Vai trò (Admin, Staff, Customer) gán qua bảng nối AspNetUserRoles.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}
