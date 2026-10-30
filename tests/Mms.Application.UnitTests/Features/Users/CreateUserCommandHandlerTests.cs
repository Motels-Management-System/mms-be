using FluentAssertions;
using NSubstitute;
using Mms.Application.Abstractions;
using Mms.Application.Common;
using Mms.Application.Features.Users.Commands;
using Mms.Application.Features.Users.Dtos;
using Mms.Application.Features.Users.Validators;

namespace Mms.Application.UnitTests.Features.Users;

public class CreateUserCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var command = new CreateUserCommand("existing@mms.local", "Password@123", "Staff");
        _identityService.GetUserByEmailAsync(command.Email).Returns(new UserDto(Guid.NewGuid(), command.Email, true, ["Staff"]));

        var handler = new CreateUserCommandHandler(_identityService, _validator);

        // Act
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email này đã được sử dụng.");
    }

    [Fact]
    public async Task HandleAsync_WhenValid_ShouldReturnCreatedUser()
    {
        // Arrange
        var command = new CreateUserCommand("newstaff@mms.local", "Password@123", "Staff");
        var userId = Guid.NewGuid();
        var expectedUser = new UserDto(userId, command.Email, true, ["Staff"]);

        _identityService.GetUserByEmailAsync(command.Email).Returns((UserDto?)null);
        _identityService.CreateUserAsync(command.Email, command.Password, command.Role)
            .Returns((true, userId, Array.Empty<string>()));
        _identityService.GetUserByIdAsync(userId).Returns(expectedUser);

        var handler = new CreateUserCommandHandler(_identityService, _validator);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedUser);
    }
}
