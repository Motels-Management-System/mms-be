using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Reservations.Commands;
using Mms.Application.Features.Reservations.Dtos;
using Mms.Application.Features.Reservations.Queries;
using Mms.Domain.Enums;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request,
        [FromServices] ICommandHandler<CreateReservationCommand, ReservationDto> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var command = new CreateReservationCommand(request.RoomId, request.TenantId, User.GetUserId(), isStaffOrAdmin);
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetReservationById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(PagedResult<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReservations(
        [FromQuery] ReservationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetReservationsQuery, PagedResult<ReservationDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetReservationsQuery(status, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(PagedResult<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetMyReservationsQuery, PagedResult<ReservationDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetMyReservationsQuery(User.GetUserId(), page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReservationById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetReservationByIdQuery, ReservationDto> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var query = new GetReservationByIdQuery(id, User.GetUserId(), isStaffOrAdmin);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmReservation(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<ConfirmReservationCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ConfirmReservationCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelReservation(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<CancelReservationCommand> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        await handler.HandleAsync(new CancelReservationCommand(id, User.GetUserId(), isStaffOrAdmin), ct);
        return NoContent();
    }
}

public sealed record CreateReservationRequest(
    Guid RoomId,
    Guid? TenantId = null);
