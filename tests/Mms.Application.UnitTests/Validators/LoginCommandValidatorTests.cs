using FluentAssertions;
using Mms.Application.Features.Auth.Commands;
using Mms.Application.Features.Auth.Validators;

namespace Mms.Application.UnitTests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Theory]
    [InlineData("", "password", "Email")]
    [InlineData("invalid-email", "password", "Email")]
    [InlineData("valid@mms.local", "", "Password")]
    public void Validate_InvalidInput_ShouldHaveError(string email, string password, string errorProperty)
    {
        var command = new LoginCommand(email, password);
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == errorProperty);
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        var command = new LoginCommand("admin@mms.local", "Admin@123456");
        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
