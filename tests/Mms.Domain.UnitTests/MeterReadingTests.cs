using FluentAssertions;
using Mms.Domain.Entities;
using Mms.Domain.Exceptions;

namespace Mms.Domain.UnitTests;

public class MeterReadingTests
{
    private static MeterReading Previous(Guid roomId, DateOnly periodMonth, decimal electricity, decimal water)
        => new(roomId, periodMonth, electricity, water);

    [Fact]
    public void Constructor_ShouldNormalizePeriodMonthToFirstDayOfMonth()
    {
        var reading = new MeterReading(Guid.NewGuid(), new DateOnly(2026, 11, 27), 100m, 20m);

        reading.PeriodMonth.Should().Be(new DateOnly(2026, 11, 1));
    }

    [Fact]
    public void Constructor_WhenValueNotLowerThanPrevious_ShouldAccept()
    {
        var roomId = Guid.NewGuid();
        var previous = Previous(roomId, new DateOnly(2026, 10, 1), 100m, 20m);

        var reading = new MeterReading(roomId, new DateOnly(2026, 11, 1), 120m, 25m, previous);

        reading.Electricity.Should().Be(120m);
        reading.Water.Should().Be(25m);
    }

    [Fact]
    public void Constructor_WhenValueLowerThanPrevious_ShouldThrowDomainException()
    {
        var roomId = Guid.NewGuid();
        var previous = Previous(roomId, new DateOnly(2026, 10, 1), 100m, 20m);

        var act = () => new MeterReading(roomId, new DateOnly(2026, 11, 1), 90m, 20m, previous);

        act.Should().Throw<DomainException>()
            .WithMessage("*nhỏ hơn chỉ số kỳ trước*");
    }

    [Fact]
    public void Constructor_WhenMeterReplaced_ShouldAllowLowerValue()
    {
        var roomId = Guid.NewGuid();
        var previous = Previous(roomId, new DateOnly(2026, 10, 1), 100m, 20m);

        var reading = new MeterReading(roomId, new DateOnly(2026, 11, 1), 5m, 1m, previous, isMeterReplaced: true);

        reading.IsMeterReplaced.Should().BeTrue();
        reading.Electricity.Should().Be(5m);
    }

    [Fact]
    public void Constructor_WhenPreviousBelongsToOtherRoom_ShouldThrowDomainException()
    {
        var previous = Previous(Guid.NewGuid(), new DateOnly(2026, 10, 1), 100m, 20m);

        var act = () => new MeterReading(Guid.NewGuid(), new DateOnly(2026, 11, 1), 120m, 25m, previous);

        act.Should().Throw<DomainException>().WithMessage("*không thuộc phòng này*");
    }

    [Fact]
    public void Constructor_WhenPreviousPeriodNotEarlier_ShouldThrowDomainException()
    {
        var roomId = Guid.NewGuid();
        var previous = Previous(roomId, new DateOnly(2026, 12, 1), 100m, 20m);

        var act = () => new MeterReading(roomId, new DateOnly(2026, 11, 1), 120m, 25m, previous);

        act.Should().Throw<DomainException>().WithMessage("*phải sau kỳ trước*");
    }

    [Fact]
    public void Constructor_WhenElectricityNegative_ShouldThrowArgumentOutOfRangeException()
    {
        decimal negative = -1m;

        var act = () => new MeterReading(Guid.NewGuid(), new DateOnly(2026, 11, 1), negative, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WhenWaterNegative_ShouldThrowArgumentOutOfRangeException()
    {
        decimal negative = -1m;

        var act = () => new MeterReading(Guid.NewGuid(), new DateOnly(2026, 11, 1), 0m, negative);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
