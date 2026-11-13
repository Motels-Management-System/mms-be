using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MeterReadings.Dtos;
using Mms.Application.Features.MeterReadings.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/meter-readings")]
public class MeterReadingsController : ControllerBase
{
    [HttpGet("missing")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(PagedResult<MissingMeterReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMissingMeterReadings(
        [FromQuery] string month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetMissingMeterReadingsQuery, PagedResult<MissingMeterReadingDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetMissingMeterReadingsQuery(month, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }
}
