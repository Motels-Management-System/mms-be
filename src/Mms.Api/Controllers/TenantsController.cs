using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Tenants.Commands;
using Mms.Application.Features.Tenants.Dtos;
using Mms.Application.Features.Tenants.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "Admin,Staff")]
public class TenantsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetTenantsQuery, PagedResult<TenantDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetTenantsQuery(keyword, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetTenantByIdQuery, TenantDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTenantByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreateTenantRequest request,
        [FromServices] ICommandHandler<CreateTenantCommand, TenantDto> handler,
        CancellationToken ct)
    {
        var command = new CreateTenantCommand(request.FullName, request.Phone);
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetTenantById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTenant(
        [FromRoute] Guid id,
        [FromBody] UpdateTenantRequest request,
        [FromServices] ICommandHandler<UpdateTenantCommand, TenantDto> handler,
        CancellationToken ct)
    {
        var command = new UpdateTenantCommand(id, request.FullName, request.Phone);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/contracts")]
    [ProducesResponseType(typeof(PagedResult<TenantContractHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantContracts(
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetTenantContractsQuery, PagedResult<TenantContractHistoryDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetTenantContractsQuery(id, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }
}

public sealed record CreateTenantRequest(string FullName, string Phone);

public sealed record UpdateTenantRequest(string FullName, string Phone);
