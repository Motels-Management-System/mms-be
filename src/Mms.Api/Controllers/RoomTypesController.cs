using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.RoomTypes.Commands;
using Mms.Application.Features.RoomTypes.Dtos;
using Mms.Application.Features.RoomTypes.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/room-types")]
public class RoomTypesController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetRoomTypesQuery, PagedResult<RoomTypeDto>> handler = default!,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(new GetRoomTypesQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomTypeById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetRoomTypeByIdQuery, RoomTypeDto> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetRoomTypeByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoomType(
        [FromBody] CreateRoomTypeRequest request,
        [FromServices] ICommandHandler<CreateRoomTypeCommand, RoomTypeDto> handler,
        CancellationToken ct)
    {
        var command = new CreateRoomTypeCommand(
            request.Name,
            request.AreaM2,
            request.MaxOccupancy,
            request.ListPrice,
            request.Description);

        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetRoomTypeById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoomType(
        [FromRoute] Guid id,
        [FromBody] UpdateRoomTypeRequest request,
        [FromServices] ICommandHandler<UpdateRoomTypeCommand, RoomTypeDto> handler,
        CancellationToken ct)
    {
        var command = new UpdateRoomTypeCommand(
            id,
            request.Name,
            request.AreaM2,
            request.MaxOccupancy,
            request.ListPrice,
            request.Description);

        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRoomType(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteRoomTypeCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteRoomTypeCommand(id), ct);
        return NoContent();
    }
}

public sealed record CreateRoomTypeRequest(
    string Name,
    decimal AreaM2,
    int MaxOccupancy,
    decimal ListPrice,
    string? Description);

public sealed record UpdateRoomTypeRequest(
    string Name,
    decimal AreaM2,
    int MaxOccupancy,
    decimal ListPrice,
    string? Description);
