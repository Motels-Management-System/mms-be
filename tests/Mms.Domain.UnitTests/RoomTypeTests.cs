using FluentAssertions;
using Mms.Domain.Entities;

namespace Mms.Domain.UnitTests;

public class RoomTypeTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var rt = new RoomType("  Phòng đơn  ", 20.5m, 2, 3500000m, "Phòng thoáng mát");

        rt.Name.Should().Be("Phòng đơn");
        rt.AreaM2.Should().Be(20.5m);
        rt.MaxOccupancy.Should().Be(2);
        rt.ListPrice.Should().Be(3500000m);
        rt.Description.Should().Be("Phòng thoáng mát");
    }

    [Theory]
    [InlineData("", 20, 2, 1000)]
    [InlineData("Phòng", 0, 2, 1000)]
    [InlineData("Phòng", -1, 2, 1000)]
    [InlineData("Phòng", 20, 0, 1000)]
    [InlineData("Phòng", 20, -1, 1000)]
    [InlineData("Phòng", 20, 2, -1)]
    public void Constructor_WithInvalidData_ShouldThrow(string name, decimal area, int occupancy, decimal price)
    {
        var act = () => new RoomType(name, area, occupancy, price);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Update_ShouldUpdateProperties()
    {
        var rt = new RoomType("Phòng đơn", 20m, 2, 3500000m);
        rt.Update("Phòng đôi", 30m, 4, 5000000m, "Mô tả mới");

        rt.Name.Should().Be("Phòng đôi");
        rt.AreaM2.Should().Be(30m);
        rt.MaxOccupancy.Should().Be(4);
        rt.ListPrice.Should().Be(5000000m);
        rt.Description.Should().Be("Mô tả mới");
    }
}
