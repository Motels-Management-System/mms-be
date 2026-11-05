using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.ValueObjects;

namespace Mms.Api.IntegrationTests;

public class AvailableRoomsEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<Guid> SeedRoomAsync(string code, RoomStatus status = RoomStatus.Available)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {code}", 25, 2, 2_000_000m, null);
            var room = new Room(code, 1, roomType.Id);
            if (status != RoomStatus.Available) room.ChangeStatus(status);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return room.Id;
        });

    [Fact]
    public async Task GetAvailableRooms_ExcludesMaintenanceRoom_KeepsOthers()
    {
        var okRoom = await SeedRoomAsync(TestApi.UniqueCode("A"));
        var maintenanceRoom = await SeedRoomAsync(TestApi.UniqueCode("M"), RoomStatus.Maintenance);

        var from = _api.Today;
        var to = from.AddMonths(1);

        var result = await TestApi.ReadJsonAsync<PagedResult<RoomDto>>(
            await _api.GetAsync($"/api/rooms/available?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

        var ids = result.Items.Select(r => r.Id).ToList();
        ids.Should().Contain(okRoom);
        ids.Should().NotContain(maintenanceRoom);
    }

    [Fact]
    public async Task GetAvailableRooms_ExcludesRoomWithOverlappingContract_KeepsAdjacentOne()
    {
        var from = _api.Today;
        var to = from.AddMonths(2);

        // Phòng bận: hợp đồng chồng [from, to).
        var busyRoomId = await SeedRoomAsync(TestApi.UniqueCode("B"));
        await _api.InDatabaseAsync(async db =>
        {
            var tenant = new Tenant("Busy Tenant", "0900000001");
            var contract = new Contract(busyRoomId, new DateRange(from.AddDays(10), from.AddDays(40)));
            contract.AddTenant(tenant.Id, isPrimary: true, maxOccupancy: 2);
            db.Tenants.Add(tenant);
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
            return true;
        });

        // Phòng kề: hợp đồng kết thúc đúng ngày from → vẫn khả dụng (khoảng nửa mở).
        var adjacentRoomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        await _api.InDatabaseAsync(async db =>
        {
            var tenant = new Tenant("Adjacent Tenant", "0900000002");
            var contract = new Contract(adjacentRoomId, new DateRange(from.AddMonths(-1), from));
            contract.AddTenant(tenant.Id, isPrimary: true, maxOccupancy: 2);
            contract.Activate();
            db.Tenants.Add(tenant);
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
            return true;
        });

        var freeRoomId = await SeedRoomAsync(TestApi.UniqueCode("D"));

        var result = await TestApi.ReadJsonAsync<PagedResult<RoomDto>>(
            await _api.GetAsync($"/api/rooms/available?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

        var ids = result.Items.Select(r => r.Id).ToList();
        ids.Should().NotContain(busyRoomId);
        ids.Should().Contain(adjacentRoomId);
        ids.Should().Contain(freeRoomId);
    }

    [Fact]
    public async Task GetAvailableRooms_ExcludesRoomWithActiveHold()
    {
        var heldRoomId = await SeedRoomAsync(TestApi.UniqueCode("H"));

        await _api.InDatabaseAsync(async db =>
        {
            var tenant = new Tenant("Hold Tenant", "0900000003");
            var adminUserId = db.Set<Mms.Infrastructure.Identity.ApplicationUser>()
                .First(u => u.Email == TestApi.AdminEmail).Id;
            var reservation = Reservation.Create(
                heldRoomId, tenant.Id, adminUserId, DateTimeOffset.UtcNow, TimeSpan.FromHours(48));

            db.Tenants.Add(tenant);
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            return true;
        });

        var from = _api.Today;
        var to = from.AddMonths(1);

        var result = await TestApi.ReadJsonAsync<PagedResult<RoomDto>>(
            await _api.GetAsync($"/api/rooms/available?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

        result.Items.Select(r => r.Id).Should().NotContain(heldRoomId);
    }

    [Fact]
    public async Task GetAvailableRooms_GuestsAboveMaxOccupancy_ReturnsEmpty()
    {
        // SeedRoomAsync tạo MaxOccupancy = 2.
        await SeedRoomAsync(TestApi.UniqueCode("G"));

        var from = _api.Today;
        var to = from.AddMonths(1);

        var result = await TestApi.ReadJsonAsync<PagedResult<RoomDto>>(
            await _api.GetAsync($"/api/rooms/available?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&guests=3"));

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableRooms_WhenToEqualsFrom_Returns400ValidationError()
    {
        var from = _api.Today;

        var response = await _api.GetAsync($"/api/rooms/available?from={from:yyyy-MM-dd}&to={from:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task GetAvailableRooms_WithoutToken_Returns200()
    {
        var from = _api.Today;
        var to = from.AddDays(5);

        var response = await _api.GetAsync(
            $"/api/rooms/available?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", TestApi.NoAuth);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
