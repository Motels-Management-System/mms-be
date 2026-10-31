using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Features.Auth.Commands;
using Mms.Application.Features.Auth.Dtos;
using Mms.Application.Features.Auth.Queries;

namespace Mms.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] ICommandHandler<RegisterCommand, AuthResponseDto> handler,
        CancellationToken ct)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.FullName, request.Phone);
        var result = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetMe), result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, AuthResponseDto> handler,
        CancellationToken ct)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromServices] ICommandHandler<RefreshTokenCommand, AuthResponseDto> handler,
        CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest? request,
        [FromServices] ICommandHandler<LogoutCommand> handler,
        CancellationToken ct)
    {
        var command = new LogoutCommand(User.GetUserId(), request?.RefreshToken);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(
        [FromServices] IQueryHandler<GetMeQuery, CurrentUserDto> handler,
        CancellationToken ct)
    {
        var query = new GetMeQuery(User.GetUserId());
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand> handler,
        CancellationToken ct)
    {
        var command = new ChangePasswordCommand(User.GetUserId(), request.CurrentPassword, request.NewPassword);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }
}

public sealed record RegisterRequest(string Email, string Password, string FullName, string Phone);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string? RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
