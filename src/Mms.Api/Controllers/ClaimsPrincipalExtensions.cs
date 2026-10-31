using System.Security.Claims;
using Mms.Application.Common;

namespace Mms.Api.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirst("sub");

        if (idClaim is null || !Guid.TryParse(idClaim.Value, out var userId))
            throw new UnauthorizedException("Không xác định được danh tính người dùng.");

        return userId;
    }
}
