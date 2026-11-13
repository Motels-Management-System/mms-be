using FluentAssertions;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;

namespace Mms.Domain.UnitTests;

public class MaintenanceRequestTests
{
    private static MaintenanceRequest OpenRequest()
        => new(Guid.NewGuid(), Guid.NewGuid(), "Hỏng bơm nước");

    [Fact]
    public void Constructor_ShouldStartAsOpenAndTrimDescription()
    {
        var request = new MaintenanceRequest(Guid.NewGuid(), Guid.NewGuid(), "  Hỏng bơm nước  ");

        request.Status.Should().Be(MaintenanceStatus.Open);
        request.Description.Should().Be("Hỏng bơm nước");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenDescriptionInvalid_ShouldThrowArgumentException(string? invalid)
    {
        var act = () => new MaintenanceRequest(Guid.NewGuid(), Guid.NewGuid(), invalid!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Start_FromOpen_ShouldMoveToInProgress()
    {
        var request = OpenRequest();

        request.Start();

        request.Status.Should().Be(MaintenanceStatus.InProgress);
    }

    [Fact]
    public void Start_FromInProgress_ShouldThrowDomainException()
    {
        var request = OpenRequest();
        request.Start();

        var act = () => request.Start();

        act.Should().Throw<DomainException>().WithMessage("*đang Open*");
    }

    [Fact]
    public void Complete_FromInProgress_ShouldMoveToDone()
    {
        var request = OpenRequest();
        request.Start();

        request.Complete();

        request.Status.Should().Be(MaintenanceStatus.Done);
    }

    [Fact]
    public void Complete_FromOpen_ShouldThrowDomainException()
    {
        var request = OpenRequest();

        var act = () => request.Complete();

        act.Should().Throw<DomainException>().WithMessage("*đang InProgress*");
    }

    [Fact]
    public void Complete_Twice_ShouldThrowDomainException()
    {
        var request = OpenRequest();
        request.Start();
        request.Complete();

        var act = () => request.Complete();

        act.Should().Throw<DomainException>().WithMessage("*đang InProgress*");
    }
}
