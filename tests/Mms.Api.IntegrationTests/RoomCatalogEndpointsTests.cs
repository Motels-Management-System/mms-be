using System.Net;
using FluentAssertions;
using Mms.Application.Common;
using Mms.Application.Features.Amenities.Dtos;
using Mms.Application.Features.Rooms.Dtos;
using Mms.Application.Features.RoomTypes.Dtos;
using Mms.Application.Features.Tenants.Dtos;
using Mms.Domain.Enums;

namespace Mms.Api.IntegrationTests;

public class RoomCatalogEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<RoomTypeDto> CreateRoomTypeAsync(string name)
    {
        var response = await _api.PostAsync("/api/room-types",
            new { name, areaM2 = 25, maxOccupancy = 2, listPrice = 3_500_000, description = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await TestApi.ReadJsonAsync<RoomTypeDto>(response);
    }

    private async Task<AmenityDto> CreateAmenityAsync(string name)
    {
        var response = await _api.PostAsync("/api/amenities", new { name });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await TestApi.ReadJsonAsync<AmenityDto>(response);
    }

    private async Task<RoomDetailDto> CreateRoomAsync(
        string code, Guid roomTypeId, params Guid[] amenityIds)
    {
        var response = await _api.PostAsync("/api/rooms",
            new { code, floor = 1, roomTypeId, amenityIds });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await TestApi.ReadJsonAsync<RoomDetailDto>(response);
    }

    private async Task<string> CreateStaffTokenAsync()
    {
        var email = $"staff{Guid.NewGuid():N}@mms.local";

        var created = await _api.PostAsync("/api/users",
            new { email, password = "Staff@123456", role = "Staff" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _api.PostAsync("/api/auth/login",
            new { email, password = "Staff@123456" }, TestApi.NoAuth);
        login.EnsureSuccessStatusCode();

        return (await TestApi.ReadJsonAsync<LoginResult>(login)).AccessToken;
    }

    private async Task<string> CreateCustomerTokenAsync()
    {
        var email = $"cust{Guid.NewGuid():N}@mms.local";

        var registered = await _api.PostAsync("/api/auth/register",
            new { email, password = "Cust@123456", fullName = "Customer One", phone = "0912345678" }, TestApi.NoAuth);
        registered.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _api.PostAsync("/api/auth/login",
            new { email, password = "Cust@123456" }, TestApi.NoAuth);
        login.EnsureSuccessStatusCode();

        return (await TestApi.ReadJsonAsync<LoginResult>(login)).AccessToken;
    }

    [Fact]
    public async Task RoomTypes_ArePublicReadableAndPaged()
    {
        var roomType = await CreateRoomTypeAsync($"Public {TestApi.UniqueCode("P")}");

        var page = await TestApi.ReadJsonAsync<PagedResult<RoomTypeDto>>(
            await _api.GetAsync("/api/room-types?page=1&pageSize=100", TestApi.NoAuth));

        page.Items.Should().ContainSingle(t => t.Id == roomType.Id);

        var single = await TestApi.ReadJsonAsync<RoomTypeDto>(
            await _api.GetAsync($"/api/room-types/{roomType.Id}", TestApi.NoAuth));
        single.Name.Should().Be(roomType.Name);
    }

    [Fact]
    public async Task DeleteRoomType_WhileRoomsStillUseIt_Returns422BusinessRule()
    {
        var roomType = await CreateRoomTypeAsync($"Busy {TestApi.UniqueCode("B")}");
        await CreateRoomAsync(TestApi.UniqueCode("R"), roomType.Id);

        var response = await _api.DeleteAsync($"/api/room-types/{roomType.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task DeleteRoomType_WhenUnused_Returns204AndThen404()
    {
        var roomType = await CreateRoomTypeAsync($"Free {TestApi.UniqueCode("F")}");

        (await _api.DeleteAsync($"/api/room-types/{roomType.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await _api.GetAsync($"/api/room-types/{roomType.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRoom_WithCodeDifferingOnlyByCase_Returns409Duplicate()
    {
        var roomType = await CreateRoomTypeAsync($"Dup {TestApi.UniqueCode("D")}");
        var code = TestApi.UniqueCode("X");

        await CreateRoomAsync(code, roomType.Id);

        var response = await _api.PostAsync("/api/rooms",
            new { code = code.ToLowerInvariant(), floor = 2, roomTypeId = roomType.Id, amenityIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("DUPLICATE");
    }

    [Fact]
    public async Task GetRooms_FiltersByFloorRoomTypeKeywordAndStatus()
    {
        var roomType = await CreateRoomTypeAsync($"Filter {TestApi.UniqueCode("S")}");
        var code = TestApi.UniqueCode("Q");
        var room = await CreateRoomAsync(code, roomType.Id);

        await _api.PostAsync($"/api/rooms/{room.Id}/status", new { status = "Maintenance" });

        var matched = await TestApi.ReadJsonAsync<PagedResult<RoomDto>>(
            await _api.GetAsync(
                $"/api/rooms?floor=1&roomTypeId={roomType.Id}&keyword={code}&status=Maintenance",
                TestApi.NoAuth));
        matched.Items.Should().ContainSingle(r => r.Id == room.Id);

        var notMatched = await TestApi.ReadJsonAsync<PagedResult<RoomDto>>(
            await _api.GetAsync("/api/rooms?status=Occupied", TestApi.NoAuth));
        notMatched.Items.Should().NotContain(r => r.Id == room.Id);
    }

    [Fact]
    public async Task UpdateRoomAmenities_ReplacesRoomAmenityList()
    {
        var roomType = await CreateRoomTypeAsync($"Amenity {TestApi.UniqueCode("N")}");
        var first = await CreateAmenityAsync($"Wifi {TestApi.UniqueCode("W")}");
        var second = await CreateAmenityAsync($"TV {TestApi.UniqueCode("V")}");
        var room = await CreateRoomAsync(TestApi.UniqueCode("M"), roomType.Id, first.Id);

        room.Amenities.Should().ContainSingle(a => a.Id == first.Id);

        var updated = await TestApi.ReadJsonAsync<RoomDetailDto>(
            await _api.PutAsync($"/api/rooms/{room.Id}/amenities", new { amenityIds = new[] { second.Id } }));

        updated.Amenities.Should().ContainSingle(a => a.Id == second.Id);
    }

    [Fact]
    public async Task ChangeRoomStatus_MaintenanceThenAvailable_Returns204()
    {
        var roomType = await CreateRoomTypeAsync($"Status {TestApi.UniqueCode("C")}");
        var room = await CreateRoomAsync(TestApi.UniqueCode("U"), roomType.Id);

        (await _api.PostAsync($"/api/rooms/{room.Id}/status", new { status = "Maintenance" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var maintenance = await TestApi.ReadJsonAsync<RoomDetailDto>(await _api.GetAsync($"/api/rooms/{room.Id}"));
        maintenance.Status.Should().Be(RoomStatus.Maintenance);

        (await _api.PostAsync($"/api/rooms/{room.Id}/status", new { status = "Available" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeRoomStatus_ToOccupied_Returns400BecauseOnlyStaffWorkflowAllowsMaintenance()
    {
        var roomType = await CreateRoomTypeAsync($"Guard {TestApi.UniqueCode("G")}");
        var room = await CreateRoomAsync(TestApi.UniqueCode("O"), roomType.Id);

        var response = await _api.PostAsync($"/api/rooms/{room.Id}/status", new { status = "Occupied" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ChangeRoomStatus_AsStaff_IsAllowedButCustomerIsForbidden()
    {
        var roomType = await CreateRoomTypeAsync($"Role {TestApi.UniqueCode("L")}");
        var room = await CreateRoomAsync(TestApi.UniqueCode("E"), roomType.Id);

        var staffToken = await CreateStaffTokenAsync();
        (await _api.PostAsync($"/api/rooms/{room.Id}/status", new { status = "Maintenance" }, staffToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var customerToken = await CreateCustomerTokenAsync();
        (await _api.PostAsync($"/api/rooms/{room.Id}/status", new { status = "Available" }, customerToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRoomById_HidesCurrentContractFromAnonymousAndShowsItToAdmin()
    {
        var code = TestApi.UniqueCode("H");
        var tenant = await TestApi.ReadJsonAsync<TenantDto>(
            await _api.PostAsync("/api/tenants", new { fullName = "Ho Van Contract", phone = "0903000111" }));

        var start = _api.Today;
        var end = _api.Today.AddMonths(2);
        var (roomId, contractId) = await _api.SeedActiveContractAsync(code, tenant.Id, start, end);

        var anonymous = await TestApi.ReadJsonAsync<RoomDetailDto>(
            await _api.GetAsync($"/api/rooms/{roomId}", TestApi.NoAuth));
        anonymous.CurrentContract.Should().BeNull();
        anonymous.RoomType.Name.Should().Be($"Type {code}");

        var admin = await TestApi.ReadJsonAsync<RoomDetailDto>(await _api.GetAsync($"/api/rooms/{roomId}"));
        admin.CurrentContract!.Id.Should().Be(contractId);
        admin.CurrentContract.StartDate.Should().Be(start);
        admin.CurrentContract.Status.Should().Be(ContractStatus.Active);
        admin.CurrentContract.DaysRemaining.Should().Be(end.DayNumber - start.DayNumber);
        admin.CurrentContract.Tenants.Should().ContainSingle()
            .Which.IsPrimary.Should().BeTrue();
    }

    private sealed record LoginResult(string AccessToken);
}
