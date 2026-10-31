using FluentAssertions;
using NSubstitute;
using Mms.Application.Abstractions;
using Mms.Application.Common;
using Mms.Application.Features.Users.Commands;
using Mms.Application.Features.Users.Validators;

namespace Mms.Application.UnitTests.Features.Users;

public class UpdateUserRolesCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly UpdateUserRolesCommandValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WhenServiceFails_ShouldThrowConflictException()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserRolesCommand(userId, ["Admin", "Staff"]);

        _identityService.UpdateUserRolesAsync(userId, Arg.Any<List<string>>())
            .Returns((false, new[] { "Không thể cập nhật vai trò." }));

        var handler = new UpdateUserRolesCommandHandler(_identityService, _validator);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Không thể cập nhật vai trò.");
    }

    [Fact]
    public async Task HandleAsync_WhenValid_ShouldSucceed()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserRolesCommand(userId, ["Staff"]);

        _identityService.UpdateUserRolesAsync(userId, Arg.Any<List<string>>())
            .Returns((true, Array.Empty<string>()));

        var handler = new UpdateUserRolesCommandHandler(_identityService, _validator);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
