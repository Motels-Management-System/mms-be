using System.Net;
using FluentAssertions;
using Mms.Application.Common;
using Mms.Application.Features.Tenants.Dtos;

namespace Mms.Api.IntegrationTests;

public class TenantEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<TenantDto> CreateTenantAsync(string fullName, string phone)
    {
        var response = await _api.PostAsync("/api/tenants", new { fullName, phone });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await TestApi.ReadJsonAsync<TenantDto>(response);
    }

    [Fact]
    public async Task CreateTenant_Returns201WithLocationAndStoredProfile()
    {
        var code = TestApi.UniqueCode("T");

        var response = await _api.PostAsync("/api/tenants", new { fullName = $"  Nguyen Van {code}  ", phone = "0901234567" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await TestApi.ReadJsonAsync<TenantDto>(response);

        response.Headers.Location!.ToString().Should().EndWith($"/api/tenants/{created.Id}");
        created.FullName.Should().Be($"Nguyen Van {code}");
        created.Phone.Should().Be("0901234567");
        created.UserId.Should().BeNull();

        var fetched = await TestApi.ReadJsonAsync<TenantDto>(await _api.GetAsync($"/api/tenants/{created.Id}"));
        fetched.Id.Should().Be(created.Id);
        fetched.FullName.Should().Be(created.FullName);
        fetched.Phone.Should().Be(created.Phone);
    }

    [Fact]
    public async Task CreateTenant_WhenPhoneHasWrongFormat_Returns400ValidationError()
    {
        var response = await _api.PostAsync("/api/tenants", new { fullName = "Tran Thi B", phone = "abc123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateTenant_WithoutToken_Returns401()
    {
        var response = await _api.PostAsync(
            "/api/tenants", new { fullName = "Anonymous Person", phone = "0901234567" }, TestApi.NoAuth);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenantById_WhenUnknown_Returns404NotFound()
    {
        var response = await _api.GetAsync($"/api/tenants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateTenant_ChangesFullNameAndPhone()
    {
        var tenant = await CreateTenantAsync("Pham Van C", "0912345678");

        var updated = await TestApi.ReadJsonAsync<TenantDto>(await _api.PutAsync(
            $"/api/tenants/{tenant.Id}", new { fullName = "Pham Van Cu", phone = "+84912345678" }));

        updated.Id.Should().Be(tenant.Id);
        updated.FullName.Should().Be("Pham Van Cu");
        updated.Phone.Should().Be("+84912345678");
    }

    [Fact]
    public async Task GetTenants_KeywordMatchesPartialNameAndPhone_IgnoringCase()
    {
        var code = TestApi.UniqueCode("K");
        await CreateTenantAsync($"Do Thi {code}", "0977000111");

        var byLowerName = await TestApi.ReadJsonAsync<PagedResult<TenantDto>>(
            await _api.GetAsync($"/api/tenants?keyword={Uri.EscapeDataString($"do thi {code}".ToLowerInvariant())}"));
        byLowerName.TotalCount.Should().Be(1);

        var byPartialPhone = await TestApi.ReadJsonAsync<PagedResult<TenantDto>>(
            await _api.GetAsync("/api/tenants?keyword=00011"));
        byPartialPhone.TotalCount.Should().Be(1);
        byPartialPhone.Items.Single().FullName.Should().Be($"Do Thi {code}");
    }

    [Fact]
    public async Task GetTenants_PageSizeOne_ReturnsPagedResultWithTotalPages()
    {
        var token = TestApi.UniqueCode("P");
        await CreateTenantAsync($"Alpha {token}", "0901111111");
        await CreateTenantAsync($"Beta {token}", "0901111112");

        var page = await TestApi.ReadJsonAsync<PagedResult<TenantDto>>(
            await _api.GetAsync($"/api/tenants?keyword={token}&page=1&pageSize=1"));

        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(1);
        page.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetTenants_PageSizeAbove100_Returns400ValidationError()
    {
        var response = await _api.GetAsync("/api/tenants?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task GetTenantContracts_ReturnsHistoryWithRoomInfoAndDaysRemaining()
    {
        var tenant = await CreateTenantAsync("Le Van History", "0988000222");
        var code = TestApi.UniqueCode("R");
        var start = _api.Today;
        var end = _api.Today.AddMonths(1);

        var (roomId, contractId) = await _api.SeedActiveContractAsync(code, tenant.Id, start, end);

        var history = await TestApi.ReadJsonAsync<PagedResult<TenantContractHistoryDto>>(
            await _api.GetAsync($"/api/tenants/{tenant.Id}/contracts"));

        history.TotalCount.Should().Be(1);

        var item = history.Items.Single();
        item.ContractId.Should().Be(contractId);
        item.RoomId.Should().Be(roomId);
        item.RoomCode.Should().Be(code);
        item.RoomTypeName.Should().Be($"Type {code}");
        item.StartDate.Should().Be(start);
        item.EndDate.Should().Be(end);
        item.Status.Should().Be(Mms.Domain.Enums.ContractStatus.Active);
        item.IsPrimary.Should().BeTrue();
        item.DaysRemaining.Should().Be((end.DayNumber - start.DayNumber));
    }

    [Fact]
    public async Task GetTenantContracts_WhenTenantUnknown_Returns404NotFound()
    {
        var response = await _api.GetAsync($"/api/tenants/{Guid.NewGuid()}/contracts");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("NOT_FOUND");
    }
}
