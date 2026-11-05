using FluentAssertions;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Domain.UnitTests;

public class HandoverRecordTests
{
    [Fact]
    public void Constructor_ShouldTrimFields()
    {
        var record = new HandoverRecord(
            Guid.NewGuid(), HandoverType.CheckIn, "  Phòng sạch, đủ nội thất  ", 2, Guid.NewGuid(), "  Ghi chú  ");

        record.RoomCondition.Should().Be("Phòng sạch, đủ nội thất");
        record.KeyCount.Should().Be(2);
        record.Notes.Should().Be("Ghi chú");
        record.Type.Should().Be(HandoverType.CheckIn);
    }

    [Fact]
    public void Constructor_WhenNotesNull_ShouldKeepNull()
    {
        var record = new HandoverRecord(Guid.NewGuid(), HandoverType.CheckOut, "Trầy tường", 0, Guid.NewGuid());

        record.Notes.Should().BeNull();
        record.KeyCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenRoomConditionInvalid_ShouldThrowArgumentException(string? invalid)
    {
        var act = () => new HandoverRecord(Guid.NewGuid(), HandoverType.CheckIn, invalid!, 1, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhenKeyCountNegative_ShouldThrowArgumentException()
    {
        var act = () => new HandoverRecord(Guid.NewGuid(), HandoverType.CheckIn, "OK", -1, Guid.NewGuid());

        act.Should().Throw<ArgumentException>().WithMessage("*âm*");
    }
}
