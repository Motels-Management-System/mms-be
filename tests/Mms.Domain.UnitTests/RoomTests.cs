using FluentAssertions;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Domain.UnitTests;

public class RoomTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        var roomTypeId = Guid.NewGuid();
        var room = new Room("  p101 ", 1, roomTypeId);

        room.Code.Should().Be("P101");
        room.Floor.Should().Be(1);
        room.RoomTypeId.Should().Be(roomTypeId);
        room.Status.Should().Be(RoomStatus.Available);
        room.Amenities.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenCodeInvalid_ShouldThrowArgumentException(string? invalidCode)
    {
        var act = () => new Room(invalidCode!, 1, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeStatus_ValidTransitions_ShouldSucceed()
    {
        var room = new Room("P101", 1, Guid.NewGuid());

        // Available -> Maintenance
        room.ChangeStatus(RoomStatus.Maintenance);
        room.Status.Should().Be(RoomStatus.Maintenance);

        // Maintenance -> Available
        room.ChangeStatus(RoomStatus.Available);
        room.Status.Should().Be(RoomStatus.Available);

        // Available -> Reserved
        room.ChangeStatus(RoomStatus.Reserved);
        room.Status.Should().Be(RoomStatus.Reserved);

        // Reserved -> Occupied
        room.ChangeStatus(RoomStatus.Occupied);
        room.Status.Should().Be(RoomStatus.Occupied);

        // Occupied -> Maintenance
        room.ChangeStatus(RoomStatus.Maintenance);
        room.Status.Should().Be(RoomStatus.Maintenance);
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ShouldThrowDomainException()
    {
        var room = new Room("P101", 1, Guid.NewGuid());
        room.ChangeStatus(RoomStatus.Maintenance);

        var act = () => room.ChangeStatus(RoomStatus.Occupied);

        act.Should().Throw<DomainException>()
            .WithMessage("*Không thể chuyển phòng P101 từ Maintenance sang Occupied.*");
    }

    [Fact]
    public void ChangeStatus_SameStatus_ShouldNoOp()
    {
        var room = new Room("P101", 1, Guid.NewGuid());
        room.ChangeStatus(RoomStatus.Available);
        room.Status.Should().Be(RoomStatus.Available);
    }

    [Fact]
    public void Update_ShouldUpdateProperties()
    {
        var room = new Room("P101", 1, Guid.NewGuid());
        var newTypeId = Guid.NewGuid();

        room.Update(" p102 ", 2, newTypeId);

        room.Code.Should().Be("P102");
        room.Floor.Should().Be(2);
        room.RoomTypeId.Should().Be(newTypeId);
    }

    [Fact]
    public void AddAmenity_ShouldAddAmenity_AndIgnoreDuplicates()
    {
        var room = new Room("P101", 1, Guid.NewGuid());
        var amenityId = Guid.NewGuid();

        room.AddAmenity(amenityId);
        room.AddAmenity(amenityId);

        room.Amenities.Should().HaveCount(1);
        room.Amenities.First().AmenityId.Should().Be(amenityId);
    }

    [Fact]
    public void UpdateAmenities_ShouldReplaceAmenities()
    {
        var room = new Room("P101", 1, Guid.NewGuid());
        var am1 = Guid.NewGuid();
        var am2 = Guid.NewGuid();
        var am3 = Guid.NewGuid();

        room.AddAmenity(am1);
        room.UpdateAmenities([am2, am3, am2]);

        room.Amenities.Should().HaveCount(2);
        room.Amenities.Select(a => a.AmenityId).Should().Contain([am2, am3]);
    }
}
