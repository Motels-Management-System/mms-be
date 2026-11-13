using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.AuditLogs.Dtos;
using Mms.Application.Features.AuditLogs.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetAuditLogsQuery(entityName, entityId, userId, from, to, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }
}
