using FluentAssertions;
using Mms.Application.Features.Tenants.Queries;
using Mms.Application.Features.Tenants.Validators;

namespace Mms.Application.UnitTests.Validators;

public class GetTenantContractsQueryValidatorTests
{
    private readonly GetTenantContractsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenPagingIsValid_ShouldPass()
    {
        var query = new GetTenantContractsQuery(Guid.NewGuid(), 1, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_ShouldFail()
    {
        var query = new GetTenantContractsQuery(Guid.Empty);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsNotPositive_ShouldFail(int page)
    {
        var query = new GetTenantContractsQuery(Guid.NewGuid(), page, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsOutOfRange_ShouldFail(int pageSize)
    {
        var query = new GetTenantContractsQuery(Guid.NewGuid(), 1, pageSize);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }
}
