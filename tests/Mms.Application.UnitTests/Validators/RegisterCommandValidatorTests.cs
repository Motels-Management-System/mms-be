using FluentAssertions;
using Mms.Application.Features.Auth.Commands;
using Mms.Application.Features.Auth.Validators;

namespace Mms.Application.UnitTests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Theory]
    [InlineData("", "Password@123", "Nguyen Van A", "0912345678", "Email")]
    [InlineData("not-an-email", "Password@123", "Nguyen Van A", "0912345678", "Email")]
    [InlineData("test@mms.local", "short", "Nguyen Van A", "0912345678", "Password")]
    [InlineData("test@mms.local", "Password@123", "", "0912345678", "FullName")]
    [InlineData("test@mms.local", "Password@123", "Nguyen Van A", "123456", "Phone")]
    [InlineData("test@mms.local", "Password@123", "Nguyen Van A", "0123456789", "Phone")] // 01 is not a valid prefix in Vietnam
    public void Validate_InvalidInput_ShouldHaveValidationError(
        string email, string password, string fullName, string phone, string expectedErrorProperty)
    {
        var command = new RegisterCommand(email, password, fullName, phone);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedErrorProperty);
    }

    [Fact]
    public void Validate_ValidInput_ShouldBeValid()
    {
        var command = new RegisterCommand("customer@mms.local", "Password@123", "Nguyen Van A", "0912345678");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
