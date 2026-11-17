using System.Net;
using FluentAssertions;
using Mms.Application.Common;
using Mms.Application.Features.Dashboard.Dtos;
using Mms.Application.Features.Reports.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.ValueObjects;

namespace Mms.Api.IntegrationTests;

public class ReportsEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<Guid> SeedRoomTypeAsync(string name)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType(name, 25, 2, 2_000_000m, null);
            db.RoomTypes.Add(roomType);
            await db.SaveChangesAsync();
            return roomType.Id;
        });

    private async Task<Guid> SeedRoomAsync(string code, Guid roomTypeId)
        => await _api.InDatabaseAsync(async db =>
        {
            var room = new Room(code, 1, roomTypeId);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return room.Id;
        });

    private async Task<Guid> SeedTenantAsync(string fullName)
        => await _api.InDatabaseAsync(async db =>
        {
            var tenant = new Tenant(fullName, $"09{Guid.NewGuid():N}"[..10]);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            return tenant.Id;
        });

    /// <summary>Tạo hợp đồng trực tiếp trong DB để kiểm soát cả Draft và Terminated.</summary>
    private async Task<Guid> SeedContractAsync(
        Guid roomId, DateOnly start, DateOnly end, Guid tenantId,
        bool activate = true, Guid? renewedFromContractId = null, DateOnly? terminateOn = null)
        => await _api.InDatabaseAsync(async db =>
        {
            var contract = new Contract(roomId, new DateRange(start, end),
                renewedFromContractId: renewedFromContractId);
            contract.AddTenant(tenantId, isPrimary: true, maxOccupancy: 4);

            if (activate) contract.Activate();
            if (terminateOn is not null) contract.Terminate(terminateOn.Value);

            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
            return contract.Id;
        });

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

    private async Task<T> GetReportAsync<T>(string path)
        => await TestApi.ReadJsonAsync<T>(await _api.GetAsync(path));

    [Fact]
    public async Task Occupancy_RoomsWithContractsInPeriod_CountsRoomDaysInBothDimensions()
    {
        // 2 phòng thuộc một loại riêng, 1 phòng có hợp đồng chiếm trọn 10 ngày của kỳ 2020-01.
        var typeName = $"Occ {TestApi.UniqueCode("T")}";
        var roomTypeId = await SeedRoomTypeAsync(typeName);
        var occupiedRoom = await SeedRoomAsync(TestApi.UniqueCode("OC"), roomTypeId);
        await SeedRoomAsync(TestApi.UniqueCode("OC"), roomTypeId);

        var tenantId = await SeedTenantAsync("Khach 2020");
        await SeedContractAsync(occupiedRoom, new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 11), tenantId);

        var byRoomType = await GetReportAsync<OccupancyReportDto>(
            "/api/reports/occupancy?from=2020-01-01&to=2020-01-11&groupBy=roomType");

        byRoomType.GroupBy.Should().Be(OccupancyGroupBy.RoomType);
        var typeBucket = byRoomType.Buckets.Single(b => b.GroupKey == typeName);
        typeBucket.RoomCount.Should().Be(2);
        typeBucket.RoomDays.Should().Be(20);
        typeBucket.OccupiedRoomDays.Should().Be(10);
        typeBucket.OccupancyRate.Should().Be(0.5m);

        // Nhóm theo tháng: không phòng nào khác trong DB có hợp đồng rơi vào tháng 1/2020.
        var byMonth = await GetReportAsync<OccupancyReportDto>(
            "/api/reports/occupancy?from=2020-01-01&to=2020-01-11");

        byMonth.GroupBy.Should().Be(OccupancyGroupBy.Month);
        var monthBucket = byMonth.Buckets.Single();
        monthBucket.GroupKey.Should().Be("2020-01");
        monthBucket.PeriodStart.Should().Be(new DateOnly(2020, 1, 1));
        monthBucket.PeriodEnd.Should().Be(new DateOnly(2020, 1, 10));
        monthBucket.OccupiedRoomDays.Should().Be(10);

        var summary = await GetReportAsync<DashboardSummaryDto>("/api/dashboard/summary");
        monthBucket.RoomDays.Should().Be(summary.TotalRooms * 10);
    }

    [Fact]
    public async Task VacancyDuration_UsesGapBetweenConsecutiveContracts()
    {
        var roomTypeId = await SeedRoomTypeAsync($"Vac {TestApi.UniqueCode("T")}");

        var tenantedRoom = await SeedRoomAsync(TestApi.UniqueCode("VA"), roomTypeId);
        var renewedRoom = await SeedRoomAsync(TestApi.UniqueCode("VA"), roomTypeId);
        var tenantId = await SeedTenantAsync("Khach 2021");

        // Phòng A: hợp đồng cách nhau 10 ngày trống.
        await SeedContractAsync(tenantedRoom, new DateOnly(2021, 1, 1), new DateOnly(2021, 2, 1), tenantId);
        await SeedContractAsync(tenantedRoom, new DateOnly(2021, 2, 11), new DateOnly(2021, 3, 1), tenantId);

        // Phòng B: gia hạn liền mạch nên khoảng trống 0 ngày.
        var firstRenewable = await SeedContractAsync(
            renewedRoom, new DateOnly(2021, 1, 1), new DateOnly(2021, 2, 1), tenantId);
        await SeedContractAsync(
            renewedRoom, new DateOnly(2021, 2, 1), new DateOnly(2021, 3, 1), tenantId,
            renewedFromContractId: firstRenewable);

        var report = await GetReportAsync<VacancyDurationReportDto>(
            "/api/reports/vacancy-duration?from=2021-01-01&to=2021-03-01");

        report.RoomCount.Should().Be(2);
        report.GapCount.Should().Be(2);
        report.AvgVacancyDaysPerGap.Should().Be(5m);

        var longest = report.Items[0];
        longest.RoomId.Should().Be(tenantedRoom);
        longest.VacancyDays.Should().Be(10);
        longest.MaxVacancyDays.Should().Be(10);
        longest.AvgVacancyDays.Should().Be(10m);

        var renewed = report.Items.Single(i => i.RoomId == renewedRoom);
        renewed.VacancyDays.Should().Be(0);
        renewed.AvgVacancyDays.Should().Be(0m);
    }

    [Fact]
    public async Task RenewalRate_SplitsRenewedReplacedAndVacantContracts()
    {
        var roomTypeId = await SeedRoomTypeAsync($"Ren {TestApi.UniqueCode("T")}");
        var oldTenant = await SeedTenantAsync("Khach cu");
        var newTenant = await SeedTenantAsync("Khach moi");

        // Phòng 1: gia hạn giữ nguyên người ở.
        var renewedRoom = await SeedRoomAsync(TestApi.UniqueCode("RN"), roomTypeId);
        await SeedContractAsync(renewedRoom, new DateOnly(2022, 1, 1), new DateOnly(2022, 2, 1), oldTenant);
        await SeedContractAsync(
            renewedRoom, new DateOnly(2022, 2, 1), new DateOnly(2022, 3, 1), oldTenant);

        // Phòng 2: phòng nối hợp đồng ngay nhưng khác người ở => thay người mới.
        var replacedRoom = await SeedRoomAsync(TestApi.UniqueCode("RN"), roomTypeId);
        await SeedContractAsync(replacedRoom, new DateOnly(2022, 1, 1), new DateOnly(2022, 2, 1), oldTenant);
        await SeedContractAsync(replacedRoom, new DateOnly(2022, 2, 1), new DateOnly(2022, 3, 1), newTenant);

        // Phòng 3: chấm dứt sớm ngày 15/02 và không có hợp đồng nối tiếp => bỏ trống.
        var vacantRoom = await SeedRoomAsync(TestApi.UniqueCode("RN"), roomTypeId);
        await SeedContractAsync(vacantRoom, new DateOnly(2022, 1, 1), new DateOnly(2022, 4, 1), newTenant,
            terminateOn: new DateOnly(2022, 2, 15));

        var report = await GetReportAsync<RenewalRateReportDto>(
            "/api/reports/renewal-rate?from=2022-01-01&to=2022-03-01");

        report.EndedContracts.Should().Be(3);
        report.RenewedContracts.Should().Be(1);
        report.ReplacedByNewTenants.Should().Be(1);
        report.LeftVacant.Should().Be(1);
        report.EarlyTerminated.Should().Be(1);
        report.TurnoverContracts.Should().Be(2);

        report.RenewalRate.Should().Be(0.3333m);
        report.TurnoverRate.Should().Be(0.6667m);
    }

    [Fact]
    public async Task Forecast_ListsRoomsWithoutContractStartingOnExpiryDate()
    {
        var today = _api.Today;
        var roomTypeId = await SeedRoomTypeAsync($"Fc {TestApi.UniqueCode("T")}");
        var tenantId = await SeedTenantAsync("Khach du bao");

        var freeSoon = await SeedRoomAsync(TestApi.UniqueCode("FC"), roomTypeId);
        await SeedContractAsync(freeSoon, today.AddDays(-10), today.AddDays(20), tenantId);

        // Đã có hợp đồng gia hạn bắt đầu đúng ngày hết hạn nên phòng không trống.
        var renewedRoom = await SeedRoomAsync(TestApi.UniqueCode("FC"), roomTypeId);
        var renewedSource = await SeedContractAsync(
            renewedRoom, today.AddDays(-10), today.AddDays(20), tenantId);
        await SeedContractAsync(
            renewedRoom, today.AddDays(20), today.AddDays(50), tenantId,
            activate: false, renewedFromContractId: renewedSource);

        // Hợp đồng kế tiếp bắt đầu muộn hơn: phòng vẫn trống trong khoảng chờ.
        var gapRoom = await SeedRoomAsync(TestApi.UniqueCode("FC"), roomTypeId);
        await SeedContractAsync(gapRoom, today.AddDays(-10), today.AddDays(25), tenantId);
        await SeedContractAsync(gapRoom, today.AddDays(40), today.AddDays(70), tenantId, activate: false);

        // Hết hạn ngoài cửa sổ 30 ngày.
        var laterRoom = await SeedRoomAsync(TestApi.UniqueCode("FC"), roomTypeId);
        await SeedContractAsync(laterRoom, today.AddDays(-10), today.AddDays(60), tenantId);

        var inThirtyDays = await GetReportAsync<PagedResult<VacancyForecastItemDto>>(
            "/api/reports/forecast?days=30&page=1&pageSize=100");

        inThirtyDays.Items.Should().Contain(i => i.RoomId == freeSoon && i.DaysUntilVacant == 20);
        inThirtyDays.Items.Should().ContainSingle(i => i.RoomId == gapRoom)
            .Which.NextContractStartDate.Should().Be(today.AddDays(40));
        inThirtyDays.Items.Should().NotContain(i => i.RoomId == renewedRoom);
        inThirtyDays.Items.Should().NotContain(i => i.RoomId == laterRoom);

        var inNinetyDays = await GetReportAsync<PagedResultShape<VacancyForecastItemDto>>(
            "/api/reports/forecast?days=90&page=1&pageSize=100");

        inNinetyDays.Items.Should().Contain(i => i.RoomId == laterRoom && i.DaysUntilVacant == 60);
        inNinetyDays.Items.Should().NotContain(i => i.RoomId == renewedRoom);
    }

    [Fact]
    public async Task Occupancy_FromNotBeforeTo_Returns400()
    {
        var response = await _api.GetAsync("/api/reports/occupancy?from=2022-03-01&to=2022-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Reports_RequireAdmin()
    {
        var staffToken = await CreateStaffTokenAsync();
        var paths = new[]
        {
            "/api/reports/occupancy?from=2020-01-01&to=2020-02-01",
            "/api/reports/vacancy-duration?from=2020-01-01&to=2020-02-01",
            "/api/reports/forecast?days=30",
            "/api/reports/renewal-rate?from=2020-01-01&to=2020-02-01",
        };

        foreach (var path in paths)
        {
            (await _api.GetAsync(path, TestApi.NoAuth)).StatusCode
                .Should().Be(HttpStatusCode.Unauthorized, because: $"{path} cần đăng nhập");

            (await _api.GetAsync(path, staffToken)).StatusCode
                .Should().Be(HttpStatusCode.Forbidden, because: $"{path} chỉ dành cho Admin");
        }
    }

    private sealed record LoginResult(string AccessToken);

    private sealed record DashboardSummaryShape(int TotalRooms);

    private sealed record PagedResultShape<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
}
