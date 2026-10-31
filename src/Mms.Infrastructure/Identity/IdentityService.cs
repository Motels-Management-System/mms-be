using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Application.Common;
using Mms.Application.Features.Users.Dtos;

namespace Mms.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IIdentityService
{
    public async Task<(bool Succeeded, Guid UserId, string[] Errors)> CreateUserAsync(
        string email, string password, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, Guid.Empty, result.Errors.Select(e => e.Description).ToArray());

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            return (false, user.Id, roleResult.Errors.Select(e => e.Description).ToArray());

        return (true, user.Id, []);
    }

    public async Task<(bool Succeeded, Guid UserId, string Email, IList<string> Roles, string[] Errors)> AuthenticateAsync(
        string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return (false, Guid.Empty, string.Empty, [], ["Email hoặc mật khẩu không chính xác."]);

        if (!user.IsActive)
            return (false, user.Id, user.Email!, [], ["Tài khoản đã bị vô hiệu hóa."]);

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
            return (false, Guid.Empty, string.Empty, [], ["Email hoặc mật khẩu không chính xác."]);

        var roles = await userManager.GetRolesAsync(user);
        return (true, user.Id, user.Email!, roles, []);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, user.IsActive, roles.ToList());
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, user.IsActive, roles.ToList());
    }

    public async Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, ["Không tìm thấy người dùng."]);

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Succeeded, string[] Errors)> SetUserActiveAsync(Guid userId, bool isActive)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, ["Không tìm thấy người dùng."]);

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Succeeded, string[] Errors)> UpdateUserRolesAsync(Guid userId, IList<string> roles)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, ["Không tìm thấy người dùng."]);

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                return (false, [$"Vai trò '{role}' không tồn tại."]);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return (false, removeResult.Errors.Select(e => e.Description).ToArray());

        var addResult = await userManager.AddToRolesAsync(user, roles);
        return (addResult.Succeeded, addResult.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await userManager.Users.CountAsync(ct);

        var users = await userManager.Users
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtoList = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dtoList.Add(new UserDto(user.Id, user.Email!, user.IsActive, roles.ToList()));
        }

        return new PagedResult<UserDto>(dtoList, page, pageSize, total);
    }
}
