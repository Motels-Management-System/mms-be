using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Common;
using Mms.Application.Features.Users.Commands;
using Mms.Application.Features.Users.Dtos;
using Mms.Application.Features.Users.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IQueryHandler<GetUsersQuery, PagedResult<UserDto>> handler = default!,
        CancellationToken ct = default)
    {
        var query = new GetUsersQuery(page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        [FromServices] ICommandHandler<CreateUserCommand, UserDto> handler,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(request.Email, request.Password, request.Role ?? "Staff");
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetUsers), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRoles(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRolesRequest request,
        [FromServices] ICommandHandler<UpdateUserRolesCommand> handler,
        CancellationToken ct)
    {
        var command = new UpdateUserRolesCommand(id, request.Roles);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(
        [FromRoute] Guid id,
        [FromBody] SetUserActiveRequest request,
        [FromServices] ICommandHandler<SetUserActiveCommand> handler,
        CancellationToken ct)
    {
        var command = new SetUserActiveCommand(id, request.IsActive);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }
}

public sealed record CreateUserRequest(string Email, string Password, string? Role);
public sealed record UpdateUserRolesRequest(IReadOnlyList<string> Roles);
public sealed record SetUserActiveRequest(bool IsActive);
