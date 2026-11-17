using System.Net;
using System.Text.Json;
using FluentAssertions;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Application.Features.Dashboard.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.ValueObjects;

namespace Mms.Api.IntegrationTests;

public class DashboardEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<Guid> SeedRoomAsync(string code, RoomStatus status)
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

    private async Task<(Guid RoomId, Guid ContractId)> SeedContractAsync(
        string code, Guid tenantId, DateOnly start, DateOnly end, bool activate)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {code}", 25, 2, 2_000_000m, null);
            var room = new Room(code, 1, roomType.Id);
            var contract = new Contract(room.Id, new DateRange(start, end));

            contract.AddTenant(tenantId, isPrimary: true, maxOccupancy: 2);
            if (activate) contract.Activate();

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            return (room.Id, contract.Id);
        });

    private async Task<Guid> CreateTenantAsync(string fullName, string phone)
    {
        var response = await _api.PostAsync("/api/tenants", new { fullName, phone });
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();
    }

    private async Task<string> CreateCustomerTokenAsync()
    {
        var email = $"cust{Guid.NewGuid():N}@mms.local";

        var registered = await _api.PostAsync("/api/auth/register",
            new { email, password = "Cust@123456", fullName = "Khách thuê", phone = "0900000000" }, TestApi.NoAuth);
        registered.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _api.PostAsync("/api/auth/login",
            new { email, password = "Cust@123456" }, TestApi.NoAuth);
        login.EnsureSuccessStatusCode();

        return (await TestApi.ReadJsonAsync<LoginResult>(login)).AccessToken;
    }

    private async Task<DashboardSummaryDto> GetSummaryAsync()
        => await TestApi.ReadJsonAsync<DashboardSummaryDto>(
            await _api.GetAsync("/api/dashboard/summary"));

    [Fact]
    public async Task Summary_CountsRoomsByStatus()
    {
        var before = await GetSummaryAsync();

        var codes = new[] { TestApi.UniqueCode("DA"), TestApi.UniqueCode("DA"), TestApi.UniqueCode("DA") };
        await SeedRoomAsync(codes[0], RoomStatus.Available);
        await SeedRoomAsync(codes[1], RoomStatus.Occupied);
        await SeedRoomAsync(codes[2], RoomStatus.Maintenance);

        var after = await GetSummaryAsync();

        after.TotalRooms.Should().Be(before.TotalRooms + 3);
        Count(after, RoomStatus.Available).Should().Be(Count(before, RoomStatus.Available) + 1);
        Count(after, RoomStatus.Occupied).Should().Be(Count(before, RoomStatus.Occupied) + 1);
        Count(after, RoomStatus.Maintenance).Should().Be(Count(before, RoomStatus.Maintenance) + 1);
        // Mọi trạng thái đều có mặt trong đáp án kể cả khi chưa có phòng nào ở trạng thái đó.
        after.ByStatus.Should().HaveCount(Enum.GetValues<RoomStatus>().Length);
    }

    [Fact]
    public async Task RoomsGrid_IncludesCurrentTenantAndExpiryCountdown()
    {
        var tenantId = await CreateTenantAsync("Nguoi o dashboard", "0901222333");
        var today = _api.Today;
        var (roomId, contractId) = await SeedContractAsync(
            TestApi.UniqueCode("DG"), tenantId, today.AddDays(-10), today.AddDays(5), activate: true);

        var page = await TestApi.ReadJsonAsync<PagedResult<DashboardRoomItemDto>>(
            await _api.GetAsync("/api/dashboard/rooms?page=1&pageSize=100"));

        page.TotalCount.Should().BeLessThanOrEqualTo(100);

        var room = page.Items.Single(i => i.RoomId == roomId);
        room.ContractId.Should().Be(contractId);
        room.PrimaryTenantName.Should().Be("Nguoi o dashboard");
        room.DaysRemaining.Should().Be(5);
        room.ExpiryLevel.Should().Be(ExpiryLevel.Red);
        room.RoomTypeName.Should().StartWith("Type ");

        // Phòng không có hợp đồng vẫn xuất hiện với các trường hợp đồng bỏ trống.
        var vacantRoomId = await SeedRoomAsync(TestApi.UniqueCode("DV"), RoomStatus.Available);
        var vacant = (await TestApi.ReadJsonAsync<PagedResult<DashboardRoomItemDto>>(
            await _api.GetAsync("/api/dashboard/rooms?page=1&pageSize=100")))
            .Items.Single(i => i.RoomId == vacantRoomId);

        vacant.ContractId.Should().BeNull();
        vacant.DaysRemaining.Should().BeNull();
        vacant.ExpiryLevel.Should().BeNull();
    }

    [Fact]
    public async Task ExpiringContracts_ListsActiveContractsSoonestFirstWithinWindow()
    {
        var tenantId = await CreateTenantAsync("Nguoi het han", "0901222444");
        var today = _api.Today;

        var (soonestRoomId, _) = await SeedContractAsync(
            TestApi.UniqueCode("DE"), tenantId, today.AddDays(-30), today.AddDays(3), activate: true);
        var (laterRoomId, _) = await SeedContractAsync(
            TestApi.UniqueCode("DE"), tenantId, today.AddDays(-30), today.AddDays(20), activate: true);
        var (farRoomId, _) = await SeedContractAsync(
            TestApi.UniqueCode("DE"), tenantId, today.AddDays(-30), today.AddDays(60), activate: true);

        var page = await TestApi.ReadJsonAsync<PagedResult<ContractSummaryDto>>(
            await _api.GetAsync("/api/dashboard/expiring-contracts?days=30&page=1&pageSize=100"));

        var ours = page.Items
            .Where(i => i.RoomId == soonestRoomId || i.RoomId == laterRoomId || i.RoomId == farRoomId)
            .ToList();

        ours.Select(i => i.RoomId).Should().BeEquivalentTo([soonestRoomId, laterRoomId]);
        ours.First().DaysRemaining.Should().Be(3);
        ours.First().ExpiryLevel.Should().Be(ExpiryLevel.Red);
        ours.Last().DaysRemaining.Should().Be(20);
        ours.Last().ExpiryLevel.Should().Be(ExpiryLevel.Yellow);

        // Hợp đồng xa hơn chỉ xuất hiện khi nới cửa sổ cảnh báo.
        var wide = await TestApi.ReadJsonAsync<PagedResult<ContractSummaryDto>>(
            await _api.GetAsync("/api/dashboard/expiring-contracts?days=90&page=1&pageSize=100"));

        wide.Items.Should().Contain(i => i.RoomId == farRoomId);
    }

    [Fact]
    public async Task ExpiringContracts_DaysOutOfRange_Returns400()
    {
        var response = await _api.GetAsync("/api/dashboard/expiring-contracts?days=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task DashboardEndpoints_ForCustomer_Returns403AndAnonymous401()
    {
        var customerToken = await CreateCustomerTokenAsync();

        foreach (var path in new[]
                 {
                     "/api/dashboard/summary",
                     "/api/dashboard/rooms",
                     "/api/dashboard/expiring-contracts",
                 })
        {
            (await _api.GetAsync(path, TestApi.NoAuth)).StatusCode
                .Should().Be(HttpStatusCode.Unauthorized, because: $"{path} cần đăng nhập");

            (await _api.GetAsync(path, customerToken)).StatusCode
                .Should().Be(HttpStatusCode.Forbidden, because: $"{path} chỉ dành cho Staff và Admin");
        }
    }

    private static int Count(DashboardSummaryDto summary, RoomStatus status)
        => summary.ByStatus.Single(i => i.Status == status).Count;

    private sealed record LoginResult(string AccessToken);
}
