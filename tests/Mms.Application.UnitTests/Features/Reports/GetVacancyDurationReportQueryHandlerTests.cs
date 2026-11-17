using FluentAssertions;
using NSubstitute;
using Mms.Application.Abstractions;
using Mms.Application.Features.Reports.Dtos;
using Mms.Application.Features.Reports.Queries;
using Mms.Application.Features.Reports.Validators;

namespace Mms.Application.UnitTests.Features.Reports;

public class GetVacancyDurationReportQueryHandlerTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 7, 1);

    private readonly IReportQueries _reports = Substitute.For<IReportQueries>();
    private readonly GetVacancyDurationReportQueryHandler _handler;

    public GetVacancyDurationReportQueryHandlerTests()
        => _handler = new GetVacancyDurationReportQueryHandler(_reports, new GetVacancyDurationReportQueryValidator());

    [Fact]
    public async Task HandleAsync_MultipleRooms_ShouldAveragePerRoomAndOverallAndOrderLongestFirst()
    {
        _reports
            .GetVacancyGapsAsync(From, To, Arg.Any<CancellationToken>())
            .Returns([
                new VacancyGapRow(Guid.NewGuid(), "A10", "Đơn", 2, 4, 3),
                new VacancyGapRow(Guid.NewGuid(), "B02", "Song đôi", 1, 20, 20)]);

        var result = await _handler.HandleAsync(new GetVacancyDurationReportQuery(From, To), CancellationToken.None);

        // Tổng 24 ngày trống trên 3 khoảng trống.
        result.RoomCount.Should().Be(2);
        result.GapCount.Should().Be(3);
        result.AvgVacancyDaysPerGap.Should().Be(8m);

        result.Items[0].RoomCode.Should().Be("B02");
        result.Items[0].AvgVacancyDays.Should().Be(20m);
        result.Items[1].AvgVacancyDays.Should().Be(2m);
    }

    [Fact]
    public async Task HandleAsync_NoGaps_ShouldReturnZeroAverage()
    {
        _reports
            .GetVacancyGapsAsync(From, To, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.HandleAsync(new GetVacancyDurationReportQuery(From, To), CancellationToken.None);

        result.RoomCount.Should().Be(0);
        result.GapCount.Should().Be(0);
        result.AvgVacancyDaysPerGap.Should().Be(0m);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PeriodLongerThanLimit_ShouldThrowValidationError()
    {
        var act = () => _handler.HandleAsync(
            new GetVacancyDurationReportQuery(From, From.AddDays(ReportPeriodRules.MaxPeriodDays + 1)),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage($"*Khoảng báo cáo tối đa {ReportPeriodRules.MaxPeriodDays} ngày.*");
    }
}
