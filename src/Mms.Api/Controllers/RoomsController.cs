using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Rooms.Commands;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Application.Features.Rooms.Queries;
using Mms.Domain.Enums;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms(
        [FromQuery] RoomStatus? status,
        [FromQuery] Guid? roomTypeId,
        [FromQuery] int? floor,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetRoomsQuery, PagedResult<RoomDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetRoomsQuery(status, roomTypeId, floor, keyword, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableRooms(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int guests = 1,
        [FromQuery] Guid? roomTypeId = null,
        [FromQuery] IReadOnlyList<Guid>? amenityIds = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetAvailableRoomsQuery, PagedResult<RoomDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetAvailableRoomsQuery(from, to, guests, roomTypeId, amenityIds, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RoomDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetRoomByIdQuery, RoomDetailDto> handler,
        CancellationToken ct)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var query = new GetRoomByIdQuery(id, isStaffOrAdmin);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoomDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoom(
        [FromBody] CreateRoomRequest request,
        [FromServices] ICommandHandler<CreateRoomCommand, RoomDetailDto> handler,
        CancellationToken ct)
    {
        var command = new CreateRoomCommand(request.Code, request.Floor, request.RoomTypeId, request.AmenityIds);
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetRoomById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoomDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoom(
        [FromRoute] Guid id,
        [FromBody] UpdateRoomRequest request,
        [FromServices] ICommandHandler<UpdateRoomCommand, RoomDetailDto> handler,
        CancellationToken ct)
    {
        var command = new UpdateRoomCommand(id, request.Code, request.Floor, request.RoomTypeId);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/amenities")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoomDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoomAmenities(
        [FromRoute] Guid id,
        [FromBody] UpdateRoomAmenitiesRequest request,
        [FromServices] ICommandHandler<UpdateRoomAmenitiesCommand, RoomDetailDto> handler,
        CancellationToken ct)
    {
        var command = new UpdateRoomAmenitiesCommand(id, request.AmenityIds);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeRoomStatus(
        [FromRoute] Guid id,
        [FromBody] ChangeRoomStatusRequest request,
        [FromServices] ICommandHandler<ChangeRoomStatusCommand> handler,
        CancellationToken ct)
    {
        var command = new ChangeRoomStatusCommand(id, request.Status);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }
}

public sealed record CreateRoomRequest(
    string Code,
    int Floor,
    Guid RoomTypeId,
    IReadOnlyList<Guid>? AmenityIds);

public sealed record UpdateRoomRequest(
    string Code,
    int Floor,
    Guid RoomTypeId);

public sealed record UpdateRoomAmenitiesRequest(
    IReadOnlyList<Guid> AmenityIds);

public sealed record ChangeRoomStatusRequest(
    RoomStatus Status);
