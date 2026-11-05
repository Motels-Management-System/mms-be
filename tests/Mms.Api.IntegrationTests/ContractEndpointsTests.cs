using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common;
using Mms.Application.Features.Contracts.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.ValueObjects;

namespace Mms.Api.IntegrationTests;

public class ContractEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    private async Task<Guid> SeedRoomAsync(string code, RoomStatus status = RoomStatus.Available)
        => await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType($"Type {code}", 25, 3, 2_000_000m, null);
            var room = new Room(code, 1, roomType.Id);
            if (status != RoomStatus.Available) room.ChangeStatus(status);

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

    private async Task<ContractDetailDto> CreateContractAsync(Guid roomId, Guid tenantId, DateOnly start, int months)
    {
        var response = await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = start,
            months,
            tenants = new[] { new { tenantId, isPrimary = true } }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await TestApi.ReadJsonAsync<ContractDetailDto>(response);
    }

    [Fact]
    public async Task CreateContract_Returns201DraftWithPrimaryTenant()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Contract Tenant", "0901234567");
        var start = _api.Today;

        var created = await CreateContractAsync(roomId, tenantId, start, 3);

        created.RoomId.Should().Be(roomId);
        created.Status.Should().Be(ContractStatus.Draft);
        created.StartDate.Should().Be(start);
        created.EndDate.Should().Be(start.AddMonths(3));
        created.Tenants.Single().TenantId.Should().Be(tenantId);
        created.Tenants.Single().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task CreateContract_OverlappingPeriod_Returns409RoomDateOverlap()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Overlap Tenant", "0901234568");
        var start = _api.Today;

        await CreateContractAsync(roomId, tenantId, start, 3);

        // Chồng lấn hoàn toàn.
        var overlap = await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = start.AddDays(10),
            months = 1,
            tenants = new[] { new { tenantId, isPrimary = true } }
        });

        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await TestApi.ReadProblemAsync(overlap)).Code.Should().Be("ROOM_DATE_OVERLAP");
    }

    [Fact]
    public async Task CreateContract_AdacentPeriodsAreAllowed()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Adjacent Tenant", "0901234569");
        var start = _api.Today;

        await CreateContractAsync(roomId, tenantId, start, 1);

        // Hợp đồng sau bắt đầu đúng ngày kết thúc hợp đồng trước: hợp lệ.
        var adjacent = await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = start.AddMonths(1),
            months = 1,
            tenants = new[] { new { tenantId, isPrimary = true } }
        });

        adjacent.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateContract_FromConfirmedReservation_ConvertsReservationInSameTransaction()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Reservation Convert Tenant", "0901234570");

        var reservationJson = await (await _api.PostAsync("/api/reservations", new { roomId, tenantId }))
            .Content.ReadAsStringAsync();
        var reservationId = JsonDocument.Parse(reservationJson).RootElement.GetProperty("id").GetGuid();
        await _api.PostAsync($"/api/reservations/{reservationId}/confirm");

        var contract = await TestApi.ReadJsonAsync<ContractDetailDto>(await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = _api.Today,
            months = 2,
            tenants = new[] { new { tenantId, isPrimary = true } },
            reservationId
        }));

        contract.ReservationId.Should().Be(reservationId);

        var reservation = await TestApi.ReadJsonAsync<Mms.Application.Features.Reservations.Dtos.ReservationDto>(
            await _api.GetAsync($"/api/reservations/{reservationId}"));
        reservation.Status.Should().Be(ReservationStatus.ConvertedToContract);
    }

    [Fact]
    public async Task CreateContract_FromPendingReservation_Returns422()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Pending Reservation Tenant", "0901234571");

        var reservationJson = await (await _api.PostAsync("/api/reservations", new { roomId, tenantId }))
            .Content.ReadAsStringAsync();
        var reservationId = JsonDocument.Parse(reservationJson).RootElement.GetProperty("id").GetGuid();

        var response = await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = _api.Today,
            months = 2,
            tenants = new[] { new { tenantId, isPrimary = true } },
            reservationId
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task CreateContract_WithoutPrimaryTenant_Returns422()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var t1 = await CreateTenantAsync("No Primary A", "0901234572");
        var t2 = await CreateTenantAsync("No Primary B", "0901234573");

        var response = await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = _api.Today,
            months = 2,
            tenants = new[] { new { tenantId = t1 }, new { tenantId = t2 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateContract_TenantsExceedMaxOccupancy_Returns422()
    {
        // SeedRoomAsync tạo MaxOccupancy = 3.
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenants = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            tenants.Add(await CreateTenantAsync($"Exceed Tenant {i}", $"090123458{i}"));
        }

        var response = await _api.PostAsync("/api/contracts", new
        {
            roomId,
            startDate = _api.Today,
            months = 2,
            tenants = tenants.Select((id, i) => new { tenantId = id, isPrimary = i == 0 }).ToArray()
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CheckIn_OnStartDateWithAvailableRoom_ActivatesContractAndRoomOccupied()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("CheckIn Tenant", "0901234590");
        var contract = await CreateContractAsync(roomId, tenantId, _api.Today, 3);

        var response = await _api.PostAsync(
            $"/api/contracts/{contract.Id}/check-in",
            new { roomCondition = "Phòng sạch, đầy đủ nội thất", keyCount = 2, notes = "Bàn giao 2 chìa" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.GetAsync($"/api/contracts/{contract.Id}"));
        detail.Status.Should().Be(ContractStatus.Active);
        detail.HandoverRecords.Should().HaveCount(1);
        detail.HandoverRecords.Single().Type.Should().Be(HandoverType.CheckIn);
        detail.HandoverRecords.Single().KeyCount.Should().Be(2);

        var roomStatus = await _api.InDatabaseAsync(async db =>
            await db.Rooms.AsNoTracking().Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync());
        roomStatus.Should().Be(RoomStatus.Occupied);
    }

    [Fact]
    public async Task CheckIn_BeforeStartDate_Returns422()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Early CheckIn Tenant", "0901234591");
        var contract = await CreateContractAsync(roomId, tenantId, _api.Today.AddDays(7), 3);

        var response = await _api.PostAsync(
            $"/api/contracts/{contract.Id}/check-in",
            new { roomCondition = "OK", keyCount = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await TestApi.ReadProblemAsync(response)).Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task Renew_CreatesDraftStartingAtOriginalEndDate()
    {
        var tenantId = await CreateTenantAsync("Renew Tenant", "0901234592");
        var start = _api.Today;
        var end = start.AddMonths(2);
        var (roomId, contractId) = await _api.SeedActiveContractAsync(TestApi.UniqueCode("C"), tenantId, start, end);

        var renewal = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.PostAsync($"/api/contracts/{contractId}/renew", new { months = 3 }));

        renewal.Id.Should().NotBe(contractId);
        renewal.Status.Should().Be(ContractStatus.Draft);
        renewal.StartDate.Should().Be(end);
        renewal.EndDate.Should().Be(end.AddMonths(3));
        renewal.RenewedFromContractId.Should().Be(contractId);
        renewal.Tenants.Should().HaveCount(1);
        renewal.Tenants.Single().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Terminate_WithEnoughNotice_Returns200AndShortensEndDate()
    {
        var tenantId = await CreateTenantAsync("Terminate Tenant", "0901234593");
        var start = _api.Today;
        var (roomId, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, start, start.AddMonths(6));

        var tooEarly = await _api.PostAsync(
            $"/api/contracts/{contractId}/terminate",
            new { terminationDate = _api.Today.AddDays(10) });
        tooEarly.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var terminationDate = _api.Today.AddDays(30);
        var terminated = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.PostAsync($"/api/contracts/{contractId}/terminate", new { terminationDate }));

        terminated.Status.Should().Be(ContractStatus.Terminated);
        terminated.EndDate.Should().Be(terminationDate);
    }

    [Fact]
    public async Task CheckOut_FullLifecycle_EndsAvailableRoomWithBothHandoverRecords()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var tenantId = await CreateTenantAsync("Lifecycle Tenant", "0901234594");
        var contract = await CreateContractAsync(roomId, tenantId, _api.Today, 6);

        await _api.PostAsync(
            $"/api/contracts/{contract.Id}/check-in",
            new { roomCondition = "Sạch sẽ", keyCount = 2 });

        await _api.PostAsync(
            $"/api/contracts/{contract.Id}/terminate",
            new { terminationDate = _api.Today.AddDays(30) });

        var response = await _api.PostAsync(
            $"/api/contracts/{contract.Id}/check-out",
            new { roomCondition = "Trầy tường phòng tắm", keyCount = 2, notes = "Cần sơn lại", toMaintenance = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.GetAsync($"/api/contracts/{contract.Id}"));
        detail.Status.Should().Be(ContractStatus.Terminated);
        detail.HandoverRecords.Select(h => h.Type).Should().ContainInOrder(HandoverType.CheckIn, HandoverType.CheckOut);

        var roomStatus = await _api.InDatabaseAsync(async db =>
            await db.Rooms.AsNoTracking().Where(r => r.Id == roomId).Select(r => r.Status).FirstAsync());
        roomStatus.Should().Be(RoomStatus.Maintenance);
    }

    [Fact]
    public async Task CheckOut_WhileContractActive_Returns422()
    {
        var tenantId = await CreateTenantAsync("Active CheckOut Tenant", "0901234595");
        var (roomId, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, _api.Today, _api.Today.AddMonths(3));

        var response = await _api.PostAsync(
            $"/api/contracts/{contractId}/check-out",
            new { roomCondition = "OK", keyCount = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ChangeRoom_ToFreeRoom_MovesContractAndSwapsRoomStatuses()
    {
        var tenantId = await CreateTenantAsync("ChangeRoom Tenant", "0901234596");
        var (oldRoomId, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, _api.Today, _api.Today.AddMonths(3));
        var newRoomId = await SeedRoomAsync(TestApi.UniqueCode("N"));

        await _api.InDatabaseAsync(async db =>
        {
            var room = await db.Rooms.FirstAsync(r => r.Id == oldRoomId);
            room.ChangeStatus(RoomStatus.Occupied);
            await db.SaveChangesAsync();
            return true;
        });

        var response = await _api.PostAsync(
            $"/api/contracts/{contractId}/change-room", new { newRoomId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.GetAsync($"/api/contracts/{contractId}"));
        detail.RoomId.Should().Be(newRoomId);

        var statuses = await _api.InDatabaseAsync(async db =>
        {
            var oldRoom = await db.Rooms.AsNoTracking().FirstAsync(r => r.Id == oldRoomId);
            var newRoom = await db.Rooms.AsNoTracking().FirstAsync(r => r.Id == newRoomId);
            return (oldRoom.Status, newRoom.Status);
        });
        statuses.Item1.Should().Be(RoomStatus.Available);
        statuses.Item2.Should().Be(RoomStatus.Occupied);
    }

    [Fact]
    public async Task ChangeRoom_ToRoomBusyInRemainingPeriod_Returns409()
    {
        var tenantId = await CreateTenantAsync("Busy ChangeRoom Tenant", "0901234597");
        var (oldRoomId, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, _api.Today, _api.Today.AddMonths(3));

        var busyNewRoomId = await _api.InDatabaseAsync(async db =>
        {
            var roomType = new RoomType("Type Busy", 25, 3, 2_000_000m, null);
            var room = new Room(TestApi.UniqueCode("B"), 1, roomType.Id);
            var tenant = new Tenant("Busy Room Tenant", "0901234604");
            var busy = new Contract(room.Id, new DateRange(_api.Today.AddDays(5), _api.Today.AddMonths(2)));
            busy.AddTenant(tenant.Id, isPrimary: true, maxOccupancy: 3);

            db.RoomTypes.Add(roomType);
            db.Rooms.Add(room);
            db.Tenants.Add(tenant);
            db.Contracts.Add(busy);
            await db.SaveChangesAsync();
            return room.Id;
        });

        var response = await _api.PostAsync(
            $"/api/contracts/{contractId}/change-room", new { newRoomId = busyNewRoomId });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddContractTenant_ThenRemove_NonPrimaryTenantLifecycle()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("C"));
        var primaryId = await CreateTenantAsync("Add Primary", "0901234598");
        var contract = await CreateContractAsync(roomId, primaryId, _api.Today, 3);

        var extraId = await CreateTenantAsync("Add Extra", "0901234599");
        var updated = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.PostAsync($"/api/contracts/{contract.Id}/tenants", new { tenantId = extraId }));
        updated.Tenants.Should().HaveCount(2);

        var remove = await _api.DeleteAsync($"/api/contracts/{contract.Id}/tenants/{extraId}");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await TestApi.ReadJsonAsync<ContractDetailDto>(
            await _api.GetAsync($"/api/contracts/{contract.Id}"));
        detail.Tenants.Select(t => t.TenantId).Should().NotContain(extraId);

        var removePrimary = await _api.DeleteAsync($"/api/contracts/{contract.Id}/tenants/{primaryId}");
        removePrimary.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetContracts_FiltersByRoomIdAndStatus()
    {
        var tenantId = await CreateTenantAsync("Filter Contracts Tenant", "0901234600");
        var (roomId, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, _api.Today, _api.Today.AddMonths(3));

        var byRoom = await TestApi.ReadJsonAsync<PagedResult<ContractSummaryDto>>(
            await _api.GetAsync($"/api/contracts?roomId={roomId}&status=Active"));

        byRoom.Items.Should().Contain(c => c.Id == contractId);
        byRoom.Items.Should().OnlyContain(c => c.RoomId == roomId && c.Status == ContractStatus.Active);
    }

    [Fact]
    public async Task GetContracts_ExpiringWithinDays_ReturnsOnlyActiveContractsEndingSoon()
    {
        var tenantId = await CreateTenantAsync("Expiring Tenant", "0901234601");
        var (roomId, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, _api.Today, _api.Today.AddDays(7));

        var result = await TestApi.ReadJsonAsync<PagedResult<ContractSummaryDto>>(
            await _api.GetAsync("/api/contracts?expiringWithinDays=30"));

        result.Items.Should().Contain(c => c.Id == contractId);
        var item = result.Items.Single(c => c.Id == contractId);
        item.ExpiryLevel.Should().Be(ExpiryLevel.Red);
        item.DaysRemaining.Should().Be(7);
    }

    [Fact]
    public async Task GetMyContracts_CustomerSeesOwnContractWithDaysRemaining()
    {
        var email = $"{TestApi.UniqueCode("u").ToLowerInvariant()}@test.local";
        var register = await _api.PostAsync(
            "/api/auth/register",
            new { email, password = "Customer@123456", fullName = "My Contract User", phone = "0901234602" },
            TestApi.NoAuth);
        register.EnsureSuccessStatusCode();
        var login = await _api.PostAsync(
            "/api/auth/login", new { email, password = "Customer@123456" }, TestApi.NoAuth);
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString()!;

        var tenantId = await _api.InDatabaseAsync(async db =>
            await db.Tenants.AsNoTracking().Where(t => t.UserId != null).Select(t => t.Id).FirstAsync());

        var start = _api.Today;
        var end = start.AddDays(20);
        var (_, contractId) = await _api.SeedActiveContractAsync(
            TestApi.UniqueCode("C"), tenantId, start, end);

        var mine = await TestApi.ReadJsonAsync<PagedResult<ContractSummaryDto>>(
            await _api.GetAsync("/api/contracts/me", token));

        mine.TotalCount.Should().Be(1);
        var item = mine.Items.Single();
        item.Id.Should().Be(contractId);
        item.DaysRemaining.Should().Be(20);
        item.ExpiryLevel.Should().Be(ExpiryLevel.Yellow);
    }

    /// <summary>
    /// Test concurrency bắt buộc: 50 request song song tạo hợp đồng cho cùng một phòng, cùng
    /// khoảng ngày → đúng 1 request 201 và 49 request 409 (exclusion constraint EX_Contracts_NoOverlap).
    /// </summary>
    [Fact]
    public async Task CreateContract_50ParallelRequestsForSameRoomSamePeriod_ExactlyOne201AndFortyNine409()
    {
        var roomId = await SeedRoomAsync(TestApi.UniqueCode("P"));
        var tenantId = await CreateTenantAsync("Parallel Tenant", "0901234603");
        var start = _api.Today;

        var body = new
        {
            roomId,
            startDate = start,
            months = 3,
            tenants = new[] { new { tenantId, isPrimary = true } }
        };

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _api.PostAsync("/api/contracts", body))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        var statusCodes = responses.Select(r => r.StatusCode).ToList();

        statusCodes.Count(s => s == HttpStatusCode.Created).Should().Be(1, $"co 1 request 201, that ra: {string.Join(", ", statusCodes.GroupBy(s => s).Select(g => $"{g.Key} x{g.Count()}"))}");
        statusCodes.Count(s => s == HttpStatusCode.Conflict).Should().Be(49);

        foreach (var conflict in responses.Where(r => r.StatusCode == HttpStatusCode.Conflict))
        {
            (await TestApi.ReadProblemAsync(conflict)).Code.Should().Be("ROOM_DATE_OVERLAP");
        }

        var contractCount = await _api.InDatabaseAsync(async db =>
            await db.Contracts.AsNoTracking().CountAsync(c => c.RoomId == roomId));
        contractCount.Should().Be(1);
    }
}
