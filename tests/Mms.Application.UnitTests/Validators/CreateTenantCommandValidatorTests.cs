using FluentAssertions;
using Mms.Application.Features.Tenants.Commands;
using Mms.Application.Features.Tenants.Validators;

namespace Mms.Application.UnitTests.Validators;

public class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    [Theory]
    [InlineData("Nguyễn Văn A", "0901234567")]
    [InlineData("Trần Thị B", "+84901234567")]
    public void Validate_WhenDataIsValid_ShouldPass(string fullName, string phone)
    {
        var result = _validator.Validate(new CreateTenantCommand(fullName, phone));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "0901234567", "FullName")]
    [InlineData("Nguyễn Văn A", "", "Phone")]
    [InlineData("Nguyễn Văn A", "901234567", "Phone")]
    [InlineData("Nguyễn Văn A", "abc", "Phone")]
    [InlineData("Nguyễn Văn A", "090123456789012345", "Phone")]
    public void Validate_WhenFieldIsInvalid_ShouldFailOnThatField(string fullName, string phone, string expectedProperty)
    {
        var result = _validator.Validate(new CreateTenantCommand(fullName, phone));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedProperty);
    }
}
