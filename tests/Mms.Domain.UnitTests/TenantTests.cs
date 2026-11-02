using FluentAssertions;
using Mms.Domain.Entities;

namespace Mms.Domain.UnitTests;

public class TenantTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var tenant = new Tenant("  Nguyễn Văn A  ", " 0901234567 ", userId);

        tenant.FullName.Should().Be("Nguyễn Văn A");
        tenant.Phone.Should().Be("0901234567");
        tenant.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("", "0901234567")]
    [InlineData("Nguyễn Văn A", "")]
    public void Constructor_WithInvalidData_ShouldThrow(string name, string phone)
    {
        var act = () => new Tenant(name, phone);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateNameAndPhone()
    {
        var tenant = new Tenant("Nguyễn Văn A", "0901234567");
        tenant.UpdateProfile("Trần Thị B", "0987654321");

        tenant.FullName.Should().Be("Trần Thị B");
        tenant.Phone.Should().Be("0987654321");
    }

    [Fact]
    public void LinkUser_ShouldSetUserId()
    {
        var tenant = new Tenant("Nguyễn Văn A", "0901234567");
        var userId = Guid.NewGuid();

        tenant.LinkUser(userId);

        tenant.UserId.Should().Be(userId);
    }
}
