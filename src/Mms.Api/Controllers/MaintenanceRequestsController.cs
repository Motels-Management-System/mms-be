using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.MaintenanceRequests.Commands;
using Mms.Application.Features.MaintenanceRequests.Dtos;
using Mms.Application.Features.MaintenanceRequests.Queries;
using Mms.Domain.Enums;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
public class MaintenanceRequestsController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(MaintenanceRequestDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMaintenanceRequest(
        [FromBody] CreateMaintenanceRequestRequest request,
        [FromServices] ICommandHandler<CreateMaintenanceRequestCommand, MaintenanceRequestDto> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var command = new CreateMaintenanceRequestCommand(
            request.RoomId, request.Description, User.GetUserId(), isStaffOrAdmin);

        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetMaintenanceRequestById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(PagedResult<MaintenanceRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceRequests(
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] Guid? roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetMaintenanceRequestsQuery, PagedResult<MaintenanceRequestDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetMaintenanceRequestsQuery(status, roomId, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(PagedResult<MaintenanceRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMaintenanceRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetMyMaintenanceRequestsQuery, PagedResult<MaintenanceRequestDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetMyMaintenanceRequestsQuery(User.GetUserId(), page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(MaintenanceRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceRequestById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetMaintenanceRequestByIdQuery, MaintenanceRequestDto> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var query = new GetMaintenanceRequestByIdQuery(id, User.GetUserId(), isStaffOrAdmin);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartMaintenanceRequest(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<StartMaintenanceRequestCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new StartMaintenanceRequestCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteMaintenanceRequest(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<CompleteMaintenanceRequestCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new CompleteMaintenanceRequestCommand(id), ct);
        return NoContent();
    }
}

public sealed record CreateMaintenanceRequestRequest(
    Guid RoomId,
    string Description);
