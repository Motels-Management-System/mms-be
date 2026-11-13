using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Features.Contracts.Commands;
using Mms.Application.Features.Reservations.Commands;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.ValueObjects;

namespace Mms.Api.IntegrationTests;

/// <summary>
/// Test logic của background job bằng cách gọi thẳng handler: job không đăng ký ở môi trường
/// Testing nên không có timer nào chạy song song với assertion.
/// </summary>
public class ExpiryJobTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private Task<int> RunReservationExpiryAsync()
        => _api.InServicesAsync(sp => sp
            .GetRequiredService<ICommandHandler<ExpireReservationsCommand, int>>()
            .HandleAsync(new ExpireReservationsCommand(), CancellationToken.None));

    private Task<int> RunContractExpiryAsync()
        => _api.InServicesAsync(sp => sp
            .GetRequiredService<ICommandHandler<ExpireContractsCommand, int>>()
            .HandleAsync(new ExpireContractsCommand(), CancellationToken.None));

    private async Task<Guid> CreateTenantAsync(string fullName, string phone)
    {
        var response = await _api.PostAsync("/api/tenants", new { fullName, phone });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task ExpireReservations_HoldPastExpiry_ExpiresReservationAndFreesReservedRoom()
    {
        var (roomId, reservationId, _) = await SeedReservationAsync(
            holdStartedHoursAgo: 5, holdDuration: TimeSpan.FromHours(4), roomStatus: RoomStatus.Reserved);

        var expired = await RunReservationExpiryAsync();

        expired.Should().Be(1);

        var after = await _api.InDatabaseAsync(async db =>
        {
            var reservation = await db.Reservations.AsNoTracking().SingleAsync(r => r.Id == reservationId);
            var room = await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == roomId);
            return (reservation.Status, room.Status);
        });

        after.Item1.Should().Be(ReservationStatus.Expired);
        after.Item2.Should().Be(RoomStatus.Available);
    }

    [Fact]
    public async Task ExpireReservations_HoldStillValid_RemainsActiveAndRoomStaysReserved()
    {
        var (roomId, reservationId, _) = await SeedReservationAsync(
            holdStartedHoursAgo: 0, holdDuration: TimeSpan.FromHours(48), roomStatus: RoomStatus.Reserved);

        var expired = await RunReservationExpiryAsync();

        expired.Should().Be(0);

        var status = await _api.InDatabaseAsync(async db =>
            (await db.Reservations.AsNoTracking().SingleAsync(r => r.Id == reservationId)).Status);
        status.Should().Be(ReservationStatus.Confirmed);

        var roomStatus = await _api.InDatabaseAsync(async db =>
            (await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == roomId)).Status);
        roomStatus.Should().Be(RoomStatus.Reserved);
    }

    [Fact]
    public async Task ExpireContracts_EndDateReached_WithoutRenewal_MarksExpiredAndKeepsRoomOccupied()
    {
        var tenantId = await CreateTenantAsync("Expire Contract Tenant", "0903334455");
        var (roomId, contractId) = await SeedContractAsync(tenantId, _api.Today.AddMonths(-1), _api.Today);

        var expired = await RunContractExpiryAsync();

        expired.Should().Be(1);

        var (contractStatus, roomStatus) = await _api.InDatabaseAsync(async db =>
        {
            var contract = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == contractId);
            var room = await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == roomId);
            return (contract.Status, room.Status);
        });

        contractStatus.Should().Be(ContractStatus.Expired);

        // Chưa check-out nên phòng vẫn Occupied, nhân viên sẽ trả phòng qua endpoint check-out.
        roomStatus.Should().Be(RoomStatus.Occupied);
    }

    [Fact]
    public async Task ExpireContracts_RenewalStartingExactlyOnEndDate_IsActivatedWithoutCheckout()
    {
        var tenantId = await CreateTenantAsync("Renewal Tenant", "0903334456");
        var (roomId, contractId) = await SeedContractAsync(tenantId, _api.Today.AddMonths(-1), _api.Today);

        var renewalId = await _api.InDatabaseAsync(async db =>
        {
            // Include Tenants để CreateRenewal sao chép được người ở sang hợp đồng mới.
            var original = await db.Contracts.Include(c => c.Tenants).SingleAsync(c => c.Id == contractId);
            var renewal = original.CreateRenewal(1);

            db.Contracts.Add(renewal);
            await db.SaveChangesAsync();
            return renewal.Id;
        });

        var expired = await RunContractExpiryAsync();

        expired.Should().Be(1);

        var statuses = await _api.InDatabaseAsync(async db =>
        {
            var original = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == contractId);
            var renewal = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == renewalId);
            var room = await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == roomId);
            return (original.Status, renewal.Status, renewal.StartDate, room.Status);
        });

        statuses.Item1.Should().Be(ContractStatus.Expired);
        statuses.Item2.Should().Be(ContractStatus.Active);
        statuses.Item3.Should().Be(_api.Today);

        // Phòng giữ nguyên Occupied, không phải check-in lại.
        statuses.Item4.Should().Be(RoomStatus.Occupied);
    }

    [Fact]
    public async Task ExpireContracts_RenewalWithGapAfterEndDate_StaysDraft()
    {
        var tenantId = await CreateTenantAsync("Gap Renewal Tenant", "0903334457");
        var (roomId, contractId) = await SeedContractAsync(tenantId, _api.Today.AddMonths(-1), _api.Today);

        var renewalId = await _api.InDatabaseAsync(async db =>
        {
            var original = await db.Contracts.SingleAsync(c => c.Id == contractId);

            // Draft bắt đầu sau ngày phòng trống 5 ngày: không liền mạch nên không tự kích hoạt.
            var renewal = new Contract(
                original.RoomId,
                new DateRange(original.EndDate.AddDays(5), original.EndDate.AddDays(35)),
                renewedFromContractId: original.Id);
            renewal.AddTenant(tenantId, isPrimary: true, maxOccupancy: 2);

            db.Contracts.Add(renewal);
            await db.SaveChangesAsync();
            return renewal.Id;
        });

        var expired = await RunContractExpiryAsync();

        expired.Should().Be(1);

        var (renewalStatus, roomStatus) = await _api.InDatabaseAsync(async db =>
        {
            var renewal = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == renewalId);
            var room = await db.Rooms.AsNoTracking().SingleAsync(r => r.Id == roomId);
            return (renewal.Status, room.Status);
        });

        renewalStatus.Should().Be(ContractStatus.Draft);
        roomStatus.Should().Be(RoomStatus.Occupied);
    }

    [Fact]
    public async Task ExpireContracts_NotYetDue_LeavesEverythingActive()
    {
        var tenantId = await CreateTenantAsync("Future End Tenant", "0903334458");
        var (_, contractId) = await SeedContractAsync(tenantId, _api.Today, _api.Today.AddMonths(2));

        var expired = await RunContractExpiryAsync();

        expired.Should().Be(0);

        var status = await _api.InDatabaseAsync(async db =>
            (await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == contractId)).Status);
        status.Should().Be(ContractStatus.Active);
    }

    private async Task<(Guid RoomId, Guid ReservationId, Guid TenantId)> SeedReservationAsync(
        int holdStartedHoursAgo, TimeSpan holdDuration, RoomStatus roomStatus)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {Guid.NewGuid():N}", 25, 2, 2_000_000m, null);
            var room = new Room(TestApi.UniqueCode("JB"), 1, roomType.Id);
            var tenant = new Tenant("Job Tenant", "090" + Guid.NewGuid().ToString("N")[..8]);
            var staffId = (await db.Users.Select(u => u.Id).FirstAsync());

            var reservation = Reservation.Create(
                room.Id,
                tenant.Id,
                staffId,
                DateTimeOffset.UtcNow.AddHours(-holdStartedHoursAgo),
                holdDuration);
            reservation.Confirm();

            room.ChangeStatus(roomStatus);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            db.Tenants.Add(tenant);
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();

            return (room.Id, reservation.Id, tenant.Id);
        });

    /// <summary>Seed hợp đồng Active trong phòng đã chuyển Occupied.</summary>
    private async Task<(Guid RoomId, Guid ContractId)> SeedContractAsync(
        Guid tenantId, DateOnly start, DateOnly end)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {Guid.NewGuid():N}", 25, 2, 2_000_000m, null);
            var room = new Room(TestApi.UniqueCode("JC"), 1, roomType.Id);
            var contract = new Contract(room.Id, new DateRange(start, end));

            contract.AddTenant(tenantId, isPrimary: true, maxOccupancy: 2);
            contract.Activate();
            room.ChangeStatus(RoomStatus.Occupied);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            return (room.Id, contract.Id);
        });
}
