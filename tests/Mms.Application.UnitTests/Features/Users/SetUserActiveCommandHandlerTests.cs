using FluentAssertions;
using NSubstitute;
using Mms.Application.Abstractions;
using Mms.Application.Common;
using Mms.Application.Features.Users.Commands;
using Mms.Application.Features.Users.Validators;

namespace Mms.Application.UnitTests.Features.Users;

public class SetUserActiveCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly SetUserActiveCommandValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ShouldThrowConflictException()
    {
        var userId = Guid.NewGuid();
        var command = new SetUserActiveCommand(userId, false);

        _identityService.SetUserActiveAsync(userId, false)
            .Returns((false, new[] { "Không tìm thấy người dùng." }));

        var handler = new SetUserActiveCommandHandler(_identityService, _validator);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Không tìm thấy người dùng.");
    }

    [Fact]
    public async Task HandleAsync_WhenValid_ShouldSucceed()
    {
        var userId = Guid.NewGuid();
        var command = new SetUserActiveCommand(userId, true);

        _identityService.SetUserActiveAsync(userId, true)
            .Returns((true, Array.Empty<string>()));

        var handler = new SetUserActiveCommandHandler(_identityService, _validator);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
