using FluentAssertions;
using Mms.Domain.Exceptions;
using Mms.Domain.ValueObjects;

namespace Mms.Domain.UnitTests;

public class DateRangeTests
{
    [Fact]
    public void DateRange_WhenEndBeforeOrEqualStart_ShouldThrowDomainException()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 1);

        var act = () => new DateRange(start, end);

        act.Should().Throw<DomainException>()
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
    }

    [Fact]
    public void DateRange_AdjacentRanges_ShouldNotOverlap()
    {
        // Half-open interval [Start, End): A ends 2026-03-01, B starts 2026-03-01
        var rangeA = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1));
        var rangeB = new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 1));

        rangeA.Overlaps(rangeB).Should().BeFalse();
        rangeB.Overlaps(rangeA).Should().BeFalse();
    }

    [Fact]
    public void DateRange_OverlappingRanges_ShouldReturnTrue()
    {
        var rangeA = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1));
        var rangeB = new DateRange(new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 1));

        rangeA.Overlaps(rangeB).Should().BeTrue();
        rangeB.Overlaps(rangeA).Should().BeTrue();
    }

    [Fact]
    public void FromMonths_ShouldAddMonthsCorrectly_HandlingEndOfMonth()
    {
        // 31/01/2026 + 1 month = 28/02/2026
        var start = new DateOnly(2026, 1, 31);
        var range = DateRange.FromMonths(start, 1);

        range.End.Should().Be(new DateOnly(2026, 2, 28));
    }
}
