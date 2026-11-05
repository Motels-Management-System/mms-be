using FluentAssertions;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.Exceptions;
using Mms.Domain.ValueObjects;

namespace Mms.Domain.UnitTests;

public class ContractTests
{
    private static Contract NewDraft(DateOnly start, int months, Guid? roomId = null)
    {
        var contract = new Contract(roomId ?? Guid.NewGuid(), DateRange.FromMonths(start, months));
        contract.AddTenant(Guid.NewGuid(), isPrimary: true, maxOccupancy: 4);
        return contract;
    }

    [Fact]
    public void CreateRenewal_ShouldStartAtEndDate_AndLinkOriginalContract()
    {
        var contract = NewDraft(new DateOnly(2026, 1, 31), 1);
        contract.Activate();
        var renewal = contract.CreateRenewal(3);

        renewal.RoomId.Should().Be(contract.RoomId);
        renewal.Status.Should().Be(ContractStatus.Draft);
        renewal.StartDate.Should().Be(new DateOnly(2026, 2, 28));
        renewal.EndDate.Should().Be(new DateOnly(2026, 5, 28));
        renewal.RenewedFromContractId.Should().Be(contract.Id);
        renewal.ReservationId.Should().BeNull();
    }

    [Fact]
    public void CreateRenewal_ShouldCopyTenants()
    {
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6);
        var extra = Guid.NewGuid();
        contract.AddTenant(extra, isPrimary: false, maxOccupancy: 4);
        contract.Activate();

        var renewal = contract.CreateRenewal(6);

        renewal.Tenants.Should().HaveCount(2);
        renewal.Tenants.Should().Contain(t => t.TenantId == extra && !t.IsPrimary);
        renewal.Tenants.Count(t => t.IsPrimary).Should().Be(1);
    }

    [Fact]
    public void CreateRenewal_WhenNotActive_ShouldThrowDomainException()
    {
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6);

        var act = () => contract.CreateRenewal(6);

        act.Should().Throw<DomainException>().WithMessage("*Active*");
    }

    [Fact]
    public void RemoveTenant_ShouldRemoveNonPrimaryTenant()
    {
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6);
        var extra = Guid.NewGuid();
        contract.AddTenant(extra, isPrimary: false, maxOccupancy: 4);

        contract.RemoveTenant(extra);

        contract.Tenants.Should().HaveCount(1);
        contract.Tenants.Should().NotContain(t => t.TenantId == extra);
    }

    [Fact]
    public void RemoveTenant_WhenTenantIsPrimary_ShouldThrowDomainException()
    {
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6);
        var primaryId = contract.Tenants.Single(t => t.IsPrimary).TenantId;

        var act = () => contract.RemoveTenant(primaryId);

        act.Should().Throw<DomainException>().WithMessage("*người đại diện*");
    }

    [Fact]
    public void RemoveTenant_WhenTenantNotInContract_ShouldThrowDomainException()
    {
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6);

        var act = () => contract.RemoveTenant(Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*không có trong hợp đồng*");
    }

    [Fact]
    public void ChangeRoom_ShouldUpdateRoomId_WhenActive()
    {
        var oldRoomId = Guid.NewGuid();
        var newRoomId = Guid.NewGuid();
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6, oldRoomId);
        contract.Activate();

        contract.ChangeRoom(newRoomId);

        contract.RoomId.Should().Be(newRoomId);
    }

    [Fact]
    public void ChangeRoom_WhenSameRoom_ShouldThrowDomainException()
    {
        var roomId = Guid.NewGuid();
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6, roomId);
        contract.Activate();

        var act = () => contract.ChangeRoom(roomId);

        act.Should().Throw<DomainException>().WithMessage("*khác phòng hiện tại*");
    }

    [Fact]
    public void ChangeRoom_WhenNotActive_ShouldThrowDomainException()
    {
        var contract = NewDraft(new DateOnly(2026, 3, 1), 6);

        var act = () => contract.ChangeRoom(Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*Active*");
    }

    [Theory]
    [InlineData(2026, 1, 29, 1, 2026, 2, 28)]   // 29/01 + 1 tháng = 28/02
    [InlineData(2026, 1, 30, 1, 2026, 2, 28)]   // 30/01 + 1 tháng = 28/02
    [InlineData(2026, 1, 31, 1, 2026, 2, 28)]   // 31/01 + 1 tháng = 28/02
    [InlineData(2028, 1, 31, 1, 2028, 2, 29)]   // năm nhuận: 31/01 + 1 tháng = 29/02
    [InlineData(2026, 1, 31, 2, 2026, 3, 31)]   // 31/01 + 2 tháng = 31/03
    [InlineData(2026, 8, 31, 6, 2027, 2, 28)] // 31/08 + 6 tháng = 28/02 năm sau
    public void FromMonths_WhenAddingMonths_ShouldClampToEndOfMonth(
        int y, int m, int d, int months, int ey, int em, int ed)
    {
        var contract = new Contract(Guid.NewGuid(), DateRange.FromMonths(new DateOnly(y, m, d), months));

        contract.StartDate.Should().Be(new DateOnly(y, m, d));
        contract.EndDate.Should().Be(new DateOnly(ey, em, ed));
    }

    [Fact]
    public void Period_WhenContractsAdjacent_ShouldNotOverlap()
    {
        // A kết thúc 01/03, B bắt đầu 01/03: hợp lệ vì khoảng là nửa mở [Start, End).
        var a = new Contract(Guid.NewGuid(), new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1)));
        var b = new Contract(Guid.NewGuid(), new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 1)));

        a.Period.Overlaps(b.Period).Should().BeFalse();
    }

    [Fact]
    public void RenewalPeriod_ShouldAttachToOriginalWithoutGapOrOverlap()
    {
        var contract = NewDraft(new DateOnly(2026, 1, 1), 2);
        contract.Activate();
        var renewal = contract.CreateRenewal(2);

        renewal.StartDate.Should().Be(contract.EndDate);
        contract.Period.Overlaps(renewal.Period).Should().BeFalse();
    }

    [Fact]
    public void Terminate_ShouldShortenEndDate_KeepsOccupiedRangeInsideConstraint()
    {
        var contract = NewDraft(new DateOnly(2026, 1, 1), 6);
        contract.Activate();

        contract.Terminate(new DateOnly(2026, 3, 15));

        contract.EndDate.Should().Be(new DateOnly(2026, 3, 15));
        contract.Status.Should().Be(ContractStatus.Terminated);
    }
}
