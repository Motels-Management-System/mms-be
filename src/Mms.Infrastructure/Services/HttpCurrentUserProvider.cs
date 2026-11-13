using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Mms.Application.Abstractions;

namespace Mms.Infrastructure.Services;

internal sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal is null) return null;

            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal.FindFirstValue("sub");

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
