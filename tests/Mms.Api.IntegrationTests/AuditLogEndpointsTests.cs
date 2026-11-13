using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common;
using Mms.Application.Features.AuditLogs.Dtos;
using Mms.Application.Features.MaintenanceRequests.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Domain.Entities;

namespace Mms.Api.IntegrationTests;

public class AuditLogEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<Guid> SeedRoomAsync(string code)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {code}", 25, 2, 2_000_000m, null);
            var room = new Room(code, 1, roomType.Id);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return room.Id;
        });

    private async Task<PagedResult<AuditLogDto>> GetLogsAsync(string queryString)
        => await TestApi.ReadJsonAsync<PagedResult<AuditLogDto>>(
            await _api.GetAsync($"/api/audit-logs{queryString}"));

    private async Task<string> RegisterCustomerAsync(string fullName, string phone)
    {
        var email = $"{TestApi.UniqueCode("u").ToLowerInvariant()}@test.local";

        (await _api.PostAsync(
            "/api/auth/register",
            new { email, password = "Customer@123456", fullName, phone },
            TestApi.NoAuth)).EnsureSuccessStatusCode();

        var login = await _api.PostAsync(
            "/api/auth/login",
            new { email, password = "Customer@123456" },
            TestApi.NoAuth);
        login.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await login.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> UpdateRoomAsync(Guid roomId, string code, int floor)
    {
        var current = await TestApi.ReadJsonAsync<RoomDetailDto>(await _api.GetAsync($"/api/rooms/{roomId}"));
        return await _api.PutAsync($"/api/rooms/{roomId}", new { code, floor, roomTypeId = current.RoomType.Id });
    }

    [Fact]
    public async Task UpdateRoom_WritesUpdatedEntryWithOldAndNewValues()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("AU"));

        var response = await UpdateRoomAsync(roomId, "A100", 5);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await GetLogsAsync($"?entityName=Room&entityId={roomId}");

        // Seed phòng trực tiếp trong DB cũng sinh một dòng Created, nên chỉ lấy dòng Updated.
        var entry = logs.Items.Should().ContainSingle(i => i.Action == "Updated").Subject;
        entry.EntityName.Should().Be("Room");
        entry.UserId.Should().NotBeNull();

        entry.NewValues.Should().NotBeNull();
        entry.NewValues!.Value.GetProperty("Floor").GetInt32().Should().Be(5);
        entry.NewValues!.Value.GetProperty("Code").GetString().Should().Be("A100");
        entry.OldValues!.Value.GetProperty("Floor").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CreateRoom_WritesCreatedEntryWithoutOldValues()
    {
        var roomTypeId = await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {TestApi.UniqueCode("AT")}", 25, 2, 2_000_000m, null);
            db.RoomTypes.Add(roomType);
            await db.SaveChangesAsync();
            return roomType.Id;
        });

        var response = await _api.PostAsync("/api/rooms", new
        {
            code = TestApi.UniqueCode("AN"),
            floor = 2,
            roomTypeId
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await TestApi.ReadJsonAsync<RoomDetailDto>(response);

        var logs = await GetLogsAsync($"?entityName=Room&entityId={created.Id}");

        var entry = logs.Items.Should().ContainSingle().Subject;
        entry.Action.Should().Be("Created");
        entry.OldValues.Should().BeNull();
        entry.NewValues!.Value.GetProperty("Code").GetString().Should().Be(created.Code);
    }

    [Fact]
    public async Task ChangeRoomStatus_WritesUpdatedEntryWithNewStatus()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("AU"));

        var response = await _api.PostAsync($"/api/rooms/{roomId}/status", new { status = "Maintenance" });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var logs = await GetLogsAsync($"?entityName=Room&entityId={roomId}");

        var entry = logs.Items.Should().ContainSingle(i => i.Action == "Updated").Subject;
        entry.NewValues!.Value.GetProperty("Status").GetString().Should().Be("Maintenance");
        entry.OldValues!.Value.GetProperty("Status").GetString().Should().Be("Available");
    }

    [Fact]
    public async Task GetAuditLogs_FilterByDateRangeKeepsOnlyEntriesInside()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("AU"));
        var created = await TestApi.ReadJsonAsync<MaintenanceRequestDto>(await _api.PostAsync(
            "/api/maintenance-requests", new { roomId, description = "Sự cố cần ghi nhật ký" }));

        var today = _api.Today;

        var inside = await GetLogsAsync($"?entityId={created.Id}&from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        inside.Items.Should().ContainSingle(i => i.Action == "Created");

        var nextWeek = today.AddDays(7);
        var outside = await GetLogsAsync($"?entityId={created.Id}&from={nextWeek:yyyy-MM-dd}&to={nextWeek:yyyy-MM-dd}");
        outside.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogs_InvalidEntityId_Returns400()
    {
        var response = await _api.GetAsync("/api/audit-logs?entityId=not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task GetAuditLogs_ByCustomer_Returns403()
    {
        var token = await RegisterCustomerAsync("Audit Customer", "0904445566");

        var response = await _api.GetAsync("/api/audit-logs", token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_FilterByUserId_ReturnsOnlyThatUser()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("AU"));
        await UpdateRoomAsync(roomId, "C300", 4);

        var adminId = await _api.InDatabaseAsync(db => db.Users
            .Where(u => u.Email == TestApi.AdminEmail)
            .Select(u => u.Id)
            .SingleAsync());

        var mine = await GetLogsAsync($"?userId={adminId}");
        mine.Items.Should().NotBeEmpty();

        var someoneElse = await GetLogsAsync($"?userId={Guid.NewGuid()}");
        someoneElse.Items.Should().BeEmpty();
    }
}
