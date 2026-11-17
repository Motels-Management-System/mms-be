using FluentAssertions;
using NSubstitute;
using Mms.Application.Abstractions;
using Mms.Application.Features.Reports.Dtos;
using Mms.Application.Features.Reports.Queries;
using Mms.Application.Features.Reports.Validators;

namespace Mms.Application.UnitTests.Features.Reports;

public class GetOccupancyReportQueryHandlerTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 3, 1);

    private readonly IReportQueries _reports = Substitute.For<IReportQueries>();
    private readonly GetOccupancyReportQueryHandler _handler;

    public GetOccupancyReportQueryHandlerTests()
        => _handler = new GetOccupancyReportQueryHandler(_reports, new GetOccupancyReportQueryValidator());

    [Fact]
    public async Task HandleAsync_TwoBuckets_ShouldComputeRatePerBucketAndOverall()
    {
        // Arrange
        _reports
            .GetOccupancyAsync(From, To, false, Arg.Any<CancellationToken>())
            .Returns([
                new OccupancyBucketRow("2026-01", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 10, 310, 248),
                new OccupancyBucketRow("2026-02", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), 10, 280, 140)]);

        var query = new GetOccupancyReportQuery(From, To);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Buckets.Select(b => b.OccupancyRate).Should().BeEquivalentTo([0.8m, 0.5m]);
        result.OverallOccupancyRate.Should().Be(Math.Round(388m / 590m, 4));
    }

    [Fact]
    public async Task HandleAsync_GroupByRoomType_ShouldAskReportsForRoomTypeDimension()
    {
        _reports
            .GetOccupancyAsync(From, To, true, Arg.Any<CancellationToken>())
            .Returns([new OccupancyBucketRow("Đơn", From, To, 5, 60, 45)]);

        var result = await _handler.HandleAsync(
            new GetOccupancyReportQuery(From, To, OccupancyGroupBy.RoomType), CancellationToken.None);

        await _reports.Received(1).GetOccupancyAsync(From, To, true, Arg.Any<CancellationToken>());
        result.Buckets.Single().GroupKey.Should().Be("Đơn");
        result.OverallOccupancyRate.Should().Be(0.75m);
    }

    [Fact]
    public async Task HandleAsync_NoBuckets_ShouldReturnZeroRateInsteadOfDivisionByZero()
    {
        _reports
            .GetOccupancyAsync(From, To, false, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.HandleAsync(new GetOccupancyReportQuery(From, To), CancellationToken.None);

        result.Buckets.Should().BeEmpty();
        result.OverallOccupancyRate.Should().Be(0m);
    }

    [Fact]
    public async Task HandleAsync_ToNotAfterFrom_ShouldThrowValidationError()
    {
        var act = () => _handler.HandleAsync(
            new GetOccupancyReportQuery(From, From), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*Ngày kết thúc phải sau ngày bắt đầu.*");
    }
}
