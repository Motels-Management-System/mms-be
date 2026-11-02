using FluentAssertions;
using Mms.Domain.Entities;

namespace Mms.Domain.UnitTests;

public class AmenityTests
{
    [Fact]
    public void Constructor_ShouldSetTrimmedName()
    {
        var amenity = new Amenity("  Wifi tốc độ cao  ");
        amenity.Name.Should().Be("Wifi tốc độ cao");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        var act = () => new Amenity(invalidName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ShouldUpdateName()
    {
        var amenity = new Amenity("Wifi");
        amenity.Update("  Máy lạnh  ");
        amenity.Name.Should().Be("Máy lạnh");
    }
}
