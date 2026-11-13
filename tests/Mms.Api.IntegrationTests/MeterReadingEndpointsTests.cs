using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common;
using Mms.Application.Features.MeterReadings.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;

namespace Mms.Api.IntegrationTests;

public class MeterReadingEndpointsTests : IAsyncLifetime
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

    private async Task<Guid> CreateTenantAsync(string fullName, string phone)
    {
        var response = await _api.PostAsync("/api/tenants", new { fullName, phone });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> PostReadingAsync(
        Guid roomId, DateOnly periodMonth, decimal electricity, decimal water, bool isMeterReplaced = false)
        => _api.PostAsync($"/api/rooms/{roomId}/meter-readings", new
        {
            periodMonth,
            electricity,
            water,
            isMeterReplaced
        });

    /// <summary>Tháng trước so với hôm nay, luôn là kỳ hợp lệ để ghi chỉ số.</summary>
    private DateOnly LastMonth() => MeterReadingMonth(_api.Today.AddMonths(-1));

    private static DateOnly MeterReadingMonth(DateOnly value) => new(value.Year, value.Month, 1);

    [Fact]
    public async Task CreateMeterReading_Returns201WithPeriodNormalizedToFirstDay()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MR"));
        var periodMonth = LastMonth();

        var response = await PostReadingAsync(roomId, periodMonth.AddDays(20), 150m, 30m);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await TestApi.ReadJsonAsync<MeterReadingDto>(response);
        created.RoomId.Should().Be(roomId);
        created.PeriodMonth.Should().Be(periodMonth);
        created.Electricity.Should().Be(150m);
        created.Water.Should().Be(30m);
        created.IsMeterReplaced.Should().BeFalse();
    }

    [Fact]
    public async Task CreateMeterReading_ForMonthAlreadyRecorded_Returns409Duplicate()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MR"));
        var periodMonth = LastMonth();

        await PostReadingAsync(roomId, periodMonth, 150m, 30m);

        var duplicate = await PostReadingAsync(roomId, periodMonth, 160m, 31m);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await TestApi.ReadProblemAsync(duplicate)).Code.Should().Be("DUPLICATE");
    }

    [Fact]
    public async Task CreateMeterReading_LowerThanPreviousWithoutMeterSwap_Returns422()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MR"));
        var previousMonth = MeterReadingMonth(_api.Today.AddMonths(-2));

        await PostReadingAsync(roomId, previousMonth, 200m, 40m);

        var lower = await PostReadingAsync(roomId, previousMonth.AddMonths(1), 100m, 41m);

        lower.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await TestApi.ReadProblemAsync(lower)).Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task CreateMeterReading_LowerThanPreviousWithMeterSwap_Returns201()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MR"));
        var previousMonth = MeterReadingMonth(_api.Today.AddMonths(-2));

        await PostReadingAsync(roomId, previousMonth, 200m, 40m);

        var replaced = await PostReadingAsync(
            roomId, previousMonth.AddMonths(1), 5m, 2m, isMeterReplaced: true);

        replaced.StatusCode.Should().Be(HttpStatusCode.Created);
        (await TestApi.ReadJsonAsync<MeterReadingDto>(replaced)).IsMeterReplaced.Should().BeTrue();
    }

    [Fact]
    public async Task CreateMeterReading_ForFutureMonth_Returns422()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MR"));

        var response = await PostReadingAsync(
            roomId, MeterReadingMonth(_api.Today.AddMonths(1)), 100m, 10m);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task CreateMeterReading_ForUnknownRoom_Returns404()
    {
        var response = await PostReadingAsync(Guid.NewGuid(), LastMonth(), 100m, 10m);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMeterReadings_ReturnsPagedNewestPeriodFirst()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("MR"));
        var first = MeterReadingMonth(_api.Today.AddMonths(-3));

        await PostReadingAsync(roomId, first, 100m, 10m);
        await PostReadingAsync(roomId, first.AddMonths(1), 130m, 14m);
        await PostReadingAsync(roomId, first.AddMonths(2), 160m, 18m);

        var page = await TestApi.ReadJsonAsync<PagedResult<MeterReadingDto>>(
            await _api.GetAsync($"/api/rooms/{roomId}/meter-readings?pageSize=2"));

        page.TotalCount.Should().Be(3);
        page.PageSize.Should().Be(2);
        page.Items.Select(i => i.PeriodMonth).Should().BeEquivalentTo(
            [first.AddMonths(2), first.AddMonths(1)], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetMeterReadings_MissingMonthFormat_Returns400()
    {
        var response = await _api.GetAsync("/api/meter-readings/missing?month=2026-13");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task MissingMeterReadings_ListsOccupiedRoomUntilReadingRecorded()
    {
        var month = LastMonth();
        var tenantId = await CreateTenantAsync("Missing Tenant", "0901112233");
        var (roomId, _) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("MS"), tenantId, month, month.AddMonths(2));

        var monthParam = month.ToString("yyyy-MM");

        var before = await TestApi.ReadJsonAsync<PagedResult<MissingMeterReadingDto>>(
            await _api.GetAsync($"/api/meter-readings/missing?month={monthParam}"));

        before.Items.Should().ContainSingle(i => i.RoomId == roomId);
        before.Items.Single(i => i.RoomId == roomId).PrimaryTenantName.Should().Be("Missing Tenant");

        await PostReadingAsync(roomId, month, 50m, 8m);

        var after = await TestApi.ReadJsonAsync<PagedResult<MissingMeterReadingDto>>(
            await _api.GetAsync($"/api/meter-readings/missing?month={monthParam}"));

        after.Items.Should().NotContain(i => i.RoomId == roomId);
    }

    [Fact]
    public async Task MissingMeterReadings_IgnoresRoomWithoutContractInThatMonth()
    {
        var month = LastMonth();
        var tenantId = await CreateTenantAsync("Future Tenant", "0901112234");

        // Hợp đồng Draft bắt đầu sau tháng cần đọc: phòng chưa có người ở kỳ đó.
        var start = month.AddMonths(1);
        var roomId = await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType("Type draft", 25, 2, 2_000_000m, null);
            var room = new Room(TestApi.UniqueCode("DR"), 1, roomType.Id);
            var contract = new Contract(room.Id, new Mms.Domain.ValueObjects.DateRange(start, start.AddMonths(3)));
            contract.AddTenant(tenantId, isPrimary: true, maxOccupancy: 2);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
            return room.Id;
        });

        var result = await TestApi.ReadJsonAsync<PagedResult<MissingMeterReadingDto>>(
            await _api.GetAsync($"/api/meter-readings/missing?month={month:yyyy-MM}"));

        result.Items.Should().NotContain(i => i.RoomId == roomId);
    }
}
