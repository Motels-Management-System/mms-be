using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Application.Features.Dashboard.Dtos;
using Mms.Application.Features.Dashboard.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromServices] IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }

    [HttpGet("rooms")]
    [ProducesResponseType(typeof(PagedResult<DashboardRoomItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetDashboardRoomsQuery, PagedResult<DashboardRoomItemDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetDashboardRoomsQuery(page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("expiring-contracts")]
    [ProducesResponseType(typeof(PagedResult<ContractSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringContracts(
        [FromQuery] int days = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetExpiringContractsQuery, PagedResult<ContractSummaryDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetExpiringContractsQuery(days, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }
}
