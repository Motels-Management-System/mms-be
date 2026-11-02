using FluentAssertions;
using Mms.Application.Features.Tenants.Commands;
using Mms.Application.Features.Tenants.Validators;

namespace Mms.Application.UnitTests.Validators;

public class UpdateTenantCommandValidatorTests
{
    private readonly UpdateTenantCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenDataIsValid_ShouldPass()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), "Nguyễn Văn A", "0901234567");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldFailOnId()
    {
        var command = new UpdateTenantCommand(Guid.Empty, "Nguyễn Văn A", "0901234567");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_WhenPhoneHasWrongFormat_ShouldFailOnPhone()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), "Nguyễn Văn A", "12345");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Phone");
    }
}
