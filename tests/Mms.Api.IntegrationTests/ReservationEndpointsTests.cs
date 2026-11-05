using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common;
using Mms.Application.Features.Reservations.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Api.IntegrationTests;

public class ReservationEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<Guid> SeedAvailableRoomAsync(string code)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {code}", 25, 2, 2_000_000m, null);
            var room = new Room(code, 1, roomType.Id);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return room.Id;
        });

    private async Task<Guid> CreateTenantAsync(string fullName, string phone)
    {
        var response = await _api.PostAsync("/api/tenants", new { fullName, phone });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
    }

    /// <summary>Đăng ký Customer mới và đăng nhập, trả về Bearer token (profile Tenant đã được liên kết).</summary>
    private async Task<string> RegisterCustomerAsync(string fullName, string phone)
    {
        var email = $"{TestApi.UniqueCode("u").ToLowerInvariant()}@test.local";

        var register = await _api.PostAsync(
            "/api/auth/register",
            new { email, password = "Customer@123456", fullName, phone },
            TestApi.NoAuth);
        register.EnsureSuccessStatusCode();

        var login = await _api.PostAsync(
            "/api/auth/login",
            new { email, password = "Customer@123456" },
            TestApi.NoAuth);
        login.EnsureSuccessStatusCode();

        var json = await login.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task CreateReservation_ByStaffForTenant_Returns201PendingWithHoldExpiry()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var tenantId = await CreateTenantAsync("Staff Hold Tenant", "0901234567");

        var response = await _api.PostAsync("/api/reservations", new { roomId, tenantId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await TestApi.ReadJsonAsync<ReservationDto>(response);
        response.Headers.Location!.ToString().Should().EndWith($"/api/reservations/{created.Id}");
        created.RoomId.Should().Be(roomId);
        created.TenantId.Should().Be(tenantId);
        created.Status.Should().Be(ReservationStatus.Pending);
        created.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddHours(47));
        created.ExpiresAt.Should().BeBefore(DateTimeOffset.UtcNow.AddHours(49));
    }

    [Fact]
    public async Task CreateReservation_TwiceForSameRoom_Returns409Duplicate()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var tenantId = await CreateTenantAsync("Duplicate Hold Tenant", "0901234568");

        var first = await _api.PostAsync("/api/reservations", new { roomId, tenantId });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _api.PostAsync("/api/reservations", new { roomId, tenantId });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await TestApi.ReadProblemAsync(second)).Code.Should().Be("DUPLICATE");
    }

    [Fact]
    public async Task CreateReservation_ByCustomerWithoutTenantId_UsesOwnTenantProfile()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var token = await RegisterCustomerAsync("Customer Self", "0912345678");

        var response = await _api.PostAsync("/api/reservations", new { roomId }, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await TestApi.ReadJsonAsync<ReservationDto>(response);
        created.TenantFullName.Should().Be("Customer Self");
        created.TenantPhone.Should().Be("0912345678");
    }

    [Fact]
    public async Task CreateReservation_CustomerPassingOtherTenantId_Returns403()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var token = await RegisterCustomerAsync("Customer Sneaky", "0912345679");
        var otherTenantId = await CreateTenantAsync("Other Tenant", "0912345680");

        var response = await _api.PostAsync("/api/reservations", new { roomId, tenantId = otherTenantId }, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task CreateReservation_ForMaintenanceRoom_Returns409()
    {
        var roomId = await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType("Type Mtn", 25, 2, 2_000_000m, null);
            var room = new Room(TestApi.UniqueCode("M"), 1, roomType.Id);
            room.ChangeStatus(RoomStatus.Maintenance);
            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return room.Id;
        });
        var tenantId = await CreateTenantAsync("Maintenance Hold Tenant", "0912345681");

        var response = await _api.PostAsync("/api/reservations", new { roomId, tenantId });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ConfirmReservation_PendingToConfirmed_SetsRoomReserved()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var tenantId = await CreateTenantAsync("Confirm Tenant", "0912345682");
        var created = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.PostAsync("/api/reservations", new { roomId, tenantId }));

        var response = await _api.PostAsync($"/api/reservations/{created.Id}/confirm");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.GetAsync($"/api/reservations/{created.Id}"));
        detail.Status.Should().Be(ReservationStatus.Confirmed);

        var roomStatus = await _api.InDatabaseAsync(async db =>
            await db.Rooms.AsNoTracking().Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync());
        roomStatus.Should().Be(RoomStatus.Reserved);
    }

    [Fact]
    public async Task CancelReservation_ByOwnerAfterConfirm_ReturnsRoomAvailable()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var token = await RegisterCustomerAsync("Cancel Owner", "0912345683");

        var created = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.PostAsync("/api/reservations", new { roomId }, token));
        await _api.PostAsync($"/api/reservations/{created.Id}/confirm");

        var response = await _api.PostAsync($"/api/reservations/{created.Id}/cancel", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.GetAsync($"/api/reservations/{created.Id}"));
        detail.Status.Should().Be(ReservationStatus.Cancelled);

        var roomStatus = await _api.InDatabaseAsync(async db =>
            await db.Rooms.AsNoTracking().Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync());
        roomStatus.Should().Be(RoomStatus.Available);
    }

    [Fact]
    public async Task CancelReservation_ByOtherCustomer_Returns403()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var token = await RegisterCustomerAsync("Cancel Victim", "0912345684");
        var otherToken = await RegisterCustomerAsync("Cancel Stranger", "0912345685");

        var created = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.PostAsync("/api/reservations", new { roomId }, token));

        var response = await _api.PostAsync($"/api/reservations/{created.Id}/cancel", null, otherToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task GetMyReservations_ReturnsOnlyOwnReservations()
    {
        var roomIdA = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var roomIdB = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var myToken = await RegisterCustomerAsync("Me Tenant", "0912345686");
        var otherToken = await RegisterCustomerAsync("Other Tenant", "0912345687");

        await _api.PostAsync("/api/reservations", new { roomId = roomIdA }, myToken);
        await _api.PostAsync("/api/reservations", new { roomId = roomIdB }, otherToken);

        var mine = await TestApi.ReadJsonAsync<PagedResult<ReservationDto>>(
            await _api.GetAsync("/api/reservations/me", myToken));

        mine.TotalCount.Should().Be(1);
        mine.Items.Single().RoomId.Should().Be(roomIdA);
    }

    [Fact]
    public async Task GetReservationById_ByOtherCustomer_Returns403()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var myToken = await RegisterCustomerAsync("Detail Owner", "0912345688");
        var otherToken = await RegisterCustomerAsync("Detail Stranger", "0912345689");

        var created = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.PostAsync("/api/reservations", new { roomId }, myToken));

        var response = await _api.GetAsync($"/api/reservations/{created.Id}", otherToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetReservations_StaffCanFilterByStatus()
    {
        var roomId = await SeedAvailableRoomAsync(TestApi.UniqueCode("R"));
        var tenantId = await CreateTenantAsync("Filter Tenant", "0912345690");
        var created = await TestApi.ReadJsonAsync<ReservationDto>(
            await _api.PostAsync("/api/reservations", new { roomId, tenantId }));
        await _api.PostAsync($"/api/reservations/{created.Id}/cancel");

        var cancelled = await TestApi.ReadJsonAsync<PagedResult<ReservationDto>>(
            await _api.GetAsync("/api/reservations?status=Cancelled"));

        cancelled.Items.Should().NotBeEmpty();
        cancelled.Items.Should().OnlyContain(r => r.Status == ReservationStatus.Cancelled);
    }
}
