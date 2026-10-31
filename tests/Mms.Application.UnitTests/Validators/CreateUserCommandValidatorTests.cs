using FluentAssertions;
using Mms.Application.Features.Users.Commands;
using Mms.Application.Features.Users.Validators;

namespace Mms.Application.UnitTests.Validators;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRoleIsInvalid_ShouldFail()
    {
        var command = new CreateUserCommand("staff@mms.local", "Password@123", "SuperAdmin");
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Staff")]
    [InlineData("Customer")]
    public void Validate_WhenRoleIsValid_ShouldPass(string role)
    {
        var command = new CreateUserCommand("user@mms.local", "Password@123", role);
        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
