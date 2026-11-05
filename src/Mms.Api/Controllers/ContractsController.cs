using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Commands;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Application.Features.Contracts.Queries;
using Mms.Domain.Enums;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ContractDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateContract(
        [FromBody] CreateContractRequest request,
        [FromServices] ICommandHandler<CreateContractCommand, ContractDetailDto> handler,
        CancellationToken ct)
    {
        var command = new CreateContractCommand(
            request.RoomId,
            request.StartDate,
            request.Months,
            request.Tenants.Select(t => new ContractTenantItem(t.TenantId, t.IsPrimary)).ToList(),
            request.ReservationId);
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetContractById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(PagedResult<ContractSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts(
        [FromQuery] ContractStatus? status,
        [FromQuery] Guid? roomId,
        [FromQuery] int? expiringWithinDays,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetContractsQuery, PagedResult<ContractSummaryDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetContractsQuery(status, roomId, expiringWithinDays, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(PagedResult<ContractSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyContracts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetMyContractsQuery, PagedResult<ContractSummaryDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetMyContractsQuery(User.GetUserId(), page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(ContractDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetContractByIdQuery, ContractDetailDto> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var query = new GetContractByIdQuery(id, User.GetUserId(), isStaffOrAdmin);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/tenants")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ContractDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddContractTenant(
        [FromRoute] Guid id,
        [FromBody] AddContractTenantRequest request,
        [FromServices] ICommandHandler<AddContractTenantCommand, ContractDetailDto> handler,
        CancellationToken ct)
    {
        var command = new AddContractTenantCommand(id, request.TenantId, request.IsPrimary);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/tenants/{tenantId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveContractTenant(
        [FromRoute] Guid id,
        [FromRoute] Guid tenantId,
        [FromServices] ICommandHandler<RemoveContractTenantCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RemoveContractTenantCommand(id, tenantId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CheckIn(
        [FromRoute] Guid id,
        [FromBody] HandoverRequest request,
        [FromServices] ICommandHandler<CheckInContractCommand> handler,
        CancellationToken ct)
    {
        var command = new CheckInContractCommand(
            id, User.GetUserId(), request.RoomCondition, request.KeyCount, request.Notes);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/renew")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ContractDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Renew(
        [FromRoute] Guid id,
        [FromBody] RenewContractRequest request,
        [FromServices] ICommandHandler<RenewContractCommand, ContractDetailDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RenewContractCommand(id, request.Months), ct);
        return CreatedAtAction(nameof(GetContractById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ContractDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Terminate(
        [FromRoute] Guid id,
        [FromBody] TerminateContractRequest request,
        [FromServices] ICommandHandler<TerminateContractCommand, ContractDetailDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new TerminateContractCommand(id, request.TerminationDate), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/check-out")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CheckOut(
        [FromRoute] Guid id,
        [FromBody] CheckOutRequest request,
        [FromServices] ICommandHandler<CheckOutContractCommand> handler,
        CancellationToken ct)
    {
        var command = new CheckOutContractCommand(
            id, User.GetUserId(), request.RoomCondition, request.KeyCount, request.Notes, request.ToMaintenance);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/change-room")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeRoom(
        [FromRoute] Guid id,
        [FromBody] ChangeContractRoomRequest request,
        [FromServices] ICommandHandler<ChangeContractRoomCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ChangeContractRoomCommand(id, request.NewRoomId), ct);
        return NoContent();
    }
}

public sealed record ContractTenantRequest(Guid TenantId, bool IsPrimary = false);

public sealed record CreateContractRequest(
    Guid RoomId,
    DateOnly StartDate,
    int Months,
    IReadOnlyList<ContractTenantRequest> Tenants,
    Guid? ReservationId = null);

public sealed record AddContractTenantRequest(Guid TenantId, bool IsPrimary = false);

public sealed record HandoverRequest(
    string RoomCondition,
    int KeyCount,
    string? Notes = null);

public sealed record RenewContractRequest(int Months);

public sealed record TerminateContractRequest(DateOnly TerminationDate);

public sealed record CheckOutRequest(
    string RoomCondition,
    int KeyCount,
    string? Notes = null,
    bool ToMaintenance = false);

public sealed record ChangeContractRoomRequest(Guid NewRoomId);
