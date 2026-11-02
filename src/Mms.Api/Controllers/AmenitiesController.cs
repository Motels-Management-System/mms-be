using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Commands;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Amenities.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/amenities")]
public class AmenitiesController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<AmenityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAmenities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromServices] IQueryHandler<GetAmenitiesQuery, PagedResult<AmenityDto>> handler = default!,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(new GetAmenitiesQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AmenityDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAmenity(
        [FromBody] CreateAmenityRequest request,
        [FromServices] ICommandHandler<CreateAmenityCommand, AmenityDto> handler,
        CancellationToken ct)
    {
        var command = new CreateAmenityCommand(request.Name);
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAmenities), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AmenityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAmenity(
        [FromRoute] Guid id,
        [FromBody] UpdateAmenityRequest request,
        [FromServices] ICommandHandler<UpdateAmenityCommand, AmenityDto> handler,
        CancellationToken ct)
    {
        var command = new UpdateAmenityCommand(id, request.Name);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAmenity(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteAmenityCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteAmenityCommand(id), ct);
        return NoContent();
    }
}

public sealed record CreateAmenityRequest(string Name);
public sealed record UpdateAmenityRequest(string Name);
