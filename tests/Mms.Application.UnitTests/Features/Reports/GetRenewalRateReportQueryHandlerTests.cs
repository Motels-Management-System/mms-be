using FluentAssertions;
using NSubstitute;
using Mms.Application.Abstractions;
using Mms.Application.Features.Reports.Dtos;
using Mms.Application.Features.Reports.Queries;
using Mms.Application.Features.Reports.Validators;

namespace Mms.Application.UnitTests.Features.Reports;

public class GetRenewalRateReportQueryHandlerTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 4, 1);

    private readonly IReportQueries _reports = Substitute.For<IReportQueries>();
    private readonly GetRenewalRateReportQueryHandler _handler;

    public GetRenewalRateReportQueryHandlerTests()
        => _handler = new GetRenewalRateReportQueryHandler(_reports, new GetRenewalRateReportQueryValidator());

    [Fact]
    public async Task HandleAsync_TenEndedContracts_ShouldSplitRenewalAndTurnover()
    {
        // 6 gia hạn, 3 thay người mới, 1 bỏ trống = 10 hợp đồng kết thúc.
        _reports
            .GetRenewalRateAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new RenewalRateRow(10, 6, 3, 1, 2));

        var result = await _handler.HandleAsync(
            new GetRenewalRateReportQuery(From, To), CancellationToken.None);

        result.RenewalRate.Should().Be(0.6m);
        result.TurnoverContracts.Should().Be(4);
        result.TurnoverRate.Should().Be(0.4m);
        result.ReplacedByNewTenants.Should().Be(3);
        result.LeftVacant.Should().Be(1);
        result.EarlyTerminated.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_NoContractEnded_ShouldReturnZeroRates()
    {
        _reports
            .GetRenewalRateAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new RenewalRateRow(0, 0, 0, 0, 0));

        var result = await _handler.HandleAsync(
            new GetRenewalRateReportQuery(From, To), CancellationToken.None);

        result.RenewalRate.Should().Be(0m);
        result.TurnoverRate.Should().Be(0m);
    }
}
