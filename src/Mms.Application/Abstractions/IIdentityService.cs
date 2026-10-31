using Mms.Application.Common;
using Mms.Application.Features.Users.Dtos;

namespace Mms.Application.Abstractions;

public interface IIdentityService
{
    Task<(bool Succeeded, Guid UserId, string[] Errors)> CreateUserAsync(string email, string password, string role);
    Task<(bool Succeeded, Guid UserId, string Email, IList<string> Roles, string[] Errors)> AuthenticateAsync(string email, string password);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<(bool Succeeded, string[] Errors)> SetUserActiveAsync(Guid userId, bool isActive);
    Task<(bool Succeeded, string[] Errors)> UpdateUserRolesAsync(Guid userId, IList<string> roles);
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken ct);
}
