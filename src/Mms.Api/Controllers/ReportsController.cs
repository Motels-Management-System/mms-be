using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reports.Dtos;
using Mms.Application.Features.Reports.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(OccupancyReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOccupancy(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] OccupancyGroupBy groupBy = OccupancyGroupBy.Month,
        [FromServices] IQueryHandler<GetOccupancyReportQuery, OccupancyReportDto> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetOccupancyReportQuery(from, to, groupBy);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("vacancy-duration")]
    [ProducesResponseType(typeof(VacancyDurationReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVacancyDuration(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromServices] IQueryHandler<GetVacancyDurationReportQuery, VacancyDurationReportDto> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetVacancyDurationReportQuery(from, to);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("forecast")]
    [ProducesResponseType(typeof(PagedResult<VacancyForecastItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVacancyForecast(
        [FromQuery] int days = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetVacancyForecastQuery, PagedResult<VacancyForecastItemDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetVacancyForecastQuery(days, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("renewal-rate")]
    [ProducesResponseType(typeof(RenewalRateReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRenewalRate(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromServices] IQueryHandler<GetRenewalRateReportQuery, RenewalRateReportDto> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetRenewalRateReportQuery(from, to);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }
}
