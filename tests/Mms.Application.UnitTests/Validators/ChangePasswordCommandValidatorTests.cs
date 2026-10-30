using FluentAssertions;
using Mms.Application.Features.Auth.Commands;
using Mms.Application.Features.Auth.Validators;

namespace Mms.Application.UnitTests.Validators;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenNewPasswordEqualsCurrent_ShouldFail()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "SamePassword@123", "SamePassword@123");
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_WhenNewPasswordTooShort_ShouldFail()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "Current@123", "Short1");
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "Current@123", "NewPassword@123");
        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
