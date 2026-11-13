using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common;
using Mms.Application.Features.MaintenanceRequests.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Api.IntegrationTests;

public class MaintenanceRequestEndpointsTests : IAsyncLifetime
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

    /// <summary>Đăng ký Customer, lấy Bearer token và Id hồ sơ Tenant của tài khoản đó.</summary>
    private async Task<(string Token, Guid TenantId)> RegisterCustomerAsync(string fullName, string phone)
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

        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString()!;

        var tenantId = await _api.InDatabaseAsync(async db =>
            (await db.Tenants.SingleAsync(t => t.Phone == phone)).Id);

        return (token, tenantId);
    }

    /// <summary>Hợp đồng Active hôm nay của tenant trong phòng đã cho, để Customer có quyền gửi yêu cầu.</summary>
    private async Task ActivateContractInRoomAsync(Guid roomId, Guid tenantId)
        => await _api.InDatabaseAsync(async db =>
        {
            var contract = new Contract(
                roomId,
                new Mms.Domain.ValueObjects.DateRange(_api.Today, _api.Today.AddMonths(1)));
            contract.AddTenant(tenantId, isPrimary: true, maxOccupancy: 2);
            contract.Activate();

            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
            return true;
        });

    private async Task<MaintenanceRequestDto> CreateByStaffAsync(Guid roomId, string description = "Hỏng đèn hành lang")
    {
        var response = await _api.PostAsync("/api/maintenance-requests", new { roomId, description });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await TestApi.ReadJsonAsync<MaintenanceRequestDto>(response);
    }

    [Fact]
    public async Task CreateMaintenanceRequest_ByStaff_Returns201Open()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));

        var created = await CreateByStaffAsync(roomId, "  Hỏng đèn hành lang  ");

        created.RoomId.Should().Be(roomId);
        created.Status.Should().Be(MaintenanceStatus.Open);
        created.Description.Should().Be("Hỏng đèn hành lang");
    }

    [Fact]
    public async Task CreateMaintenanceRequest_ByCustomerForOwnRoom_Returns201()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var (token, tenantId) = await RegisterCustomerAsync("Maintenance Tenant", "0902223344");
        await ActivateContractInRoomAsync(roomId, tenantId);

        var response = await _api.PostAsync(
            "/api/maintenance-requests", new { roomId, description = "Tắc bồn cầu" }, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await TestApi.ReadJsonAsync<MaintenanceRequestDto>(response)).RoomId.Should().Be(roomId);
    }

    [Fact]
    public async Task CreateMaintenanceRequest_ByCustomerForOtherRoom_Returns403()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var otherRoomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var (token, tenantId) = await RegisterCustomerAsync("Other Maintenance Tenant", "0902223345");
        await ActivateContractInRoomAsync(roomId, tenantId);

        var response = await _api.PostAsync(
            "/api/maintenance-requests", new { roomId = otherRoomId, description = "Hỏng quạt" }, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task StartThenComplete_MovesRequestThroughStatuses()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var created = await CreateByStaffAsync(roomId);

        (await _api.PostAsync($"/api/maintenance-requests/{created.Id}/start")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var inProgress = await TestApi.ReadJsonAsync<MaintenanceRequestDto>(
            await _api.GetAsync($"/api/maintenance-requests/{created.Id}"));
        inProgress.Status.Should().Be(MaintenanceStatus.InProgress);

        (await _api.PostAsync($"/api/maintenance-requests/{created.Id}/complete")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var done = await TestApi.ReadJsonAsync<MaintenanceRequestDto>(
            await _api.GetAsync($"/api/maintenance-requests/{created.Id}"));
        done.Status.Should().Be(MaintenanceStatus.Done);
    }

    [Fact]
    public async Task Start_Twice_Returns422BusinessRuleViolation()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var created = await CreateByStaffAsync(roomId);

        await _api.PostAsync($"/api/maintenance-requests/{created.Id}/start");

        var second = await _api.PostAsync($"/api/maintenance-requests/{created.Id}/start");

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await TestApi.ReadProblemAsync(second)).Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task Complete_WhileStillOpen_Returns422()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var created = await CreateByStaffAsync(roomId);

        var response = await _api.PostAsync($"/api/maintenance-requests/{created.Id}/complete");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Start_ByCustomer_Returns403()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var created = await CreateByStaffAsync(roomId);
        var (token, _) = await RegisterCustomerAsync("No Permission Tenant", "0902223346");

        var response = await _api.PostAsync($"/api/maintenance-requests/{created.Id}/start", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMaintenanceRequests_FiltersByStatusAndRoom()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var otherRoomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));

        var open = await CreateByStaffAsync(roomId, "Chưa sửa");
        await CreateByStaffAsync(otherRoomId, "Đang sửa");
        await _api.PostAsync($"/api/maintenance-requests/{open.Id}/start");

        var byRoom = await TestApi.ReadJsonAsync<PagedResult<MaintenanceRequestDto>>(
            await _api.GetAsync($"/api/maintenance-requests?roomId={roomId}"));
        byRoom.Items.Should().OnlyContain(i => i.RoomId == roomId);

        var byStatus = await TestApi.ReadJsonAsync<PagedResult<MaintenanceRequestDto>>(
            await _api.GetAsync("/api/maintenance-requests?status=InProgress"));
        byStatus.Items.Should().OnlyContain(i => i.Status == MaintenanceStatus.InProgress);

        var done = await TestApi.ReadJsonAsync<PagedResult<MaintenanceRequestDto>>(
            await _api.GetAsync("/api/maintenance-requests?status=Done"));
        done.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMaintenanceRequests_ByCustomer_Returns403()
    {
        var (token, _) = await RegisterCustomerAsync("List Denied Tenant", "0902223347");

        var response = await _api.GetAsync("/api/maintenance-requests", token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMyMaintenanceRequests_IncludesOwnAndStaffCreatedForOwnRoom()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var otherRoomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var (token, tenantId) = await RegisterCustomerAsync("My Requests Tenant", "0902223348");
        await ActivateContractInRoomAsync(roomId, tenantId);

        var createdByStaffForMyRoom = await CreateByStaffAsync(roomId, "Quạt trần kêu");
        var createdByCustomer = await _api.PostAsync(
            "/api/maintenance-requests", new { roomId, description = "Đèn nhà vệ sinh chớp" }, token);
        createdByCustomer.EnsureSuccessStatusCode();
        var customerId = (await TestApi.ReadJsonAsync<MaintenanceRequestDto>(createdByCustomer)).Id;

        await CreateByStaffAsync(otherRoomId, "Phòng khác");

        var mine = await TestApi.ReadJsonAsync<PagedResult<MaintenanceRequestDto>>(
            await _api.GetAsync("/api/maintenance-requests/me", token));

        mine.Items.Select(i => i.Id).Should().BeEquivalentTo([createdByStaffForMyRoom.Id, customerId]);
        mine.Items.Should().OnlyContain(i => i.RoomId == roomId);
    }

    [Fact]
    public async Task GetMaintenanceRequestById_OtherCustomersRequest_Returns403()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MT"));
        var (token, _) = await RegisterCustomerAsync("Stranger Tenant", "0902223349");

        var created = await CreateByStaffAsync(roomId, "Sự cố của người khác");

        var response = await _api.GetAsync($"/api/maintenance-requests/{created.Id}", token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
