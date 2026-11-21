using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mms.Application.Abstractions;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Domain.ValueObjects;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seed dữ liệu mẫu phong phú bằng Bogus cho môi trường Development:
/// - 5 loại phòng, 10 tiện nghi
/// - 50 phòng trải đều 5 tầng (mỗi tầng 10 phòng)
/// - ~50 người thuê (tên tiếng Việt)
/// - ~2,000+ hợp đồng không chồng lấn cho từng phòng (trải dài từ 2022 đến nay)
/// - Biên bản bàn giao nhận/trả phòng
/// - Chỉ số điện nước theo tháng
/// - Yêu cầu bảo trì và đặt giữ phòng
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Chỉ seed nếu DB chưa có phòng nào
        if (await context.Rooms.AnyAsync(ct))
        {
            logger.LogInformation("Database đã có dữ liệu phòng, bỏ qua DevelopmentDataSeeder.");
            return;
        }

        logger.LogInformation("Bắt đầu sinh dữ liệu mẫu Development bằng Bogus...");

        // 1. Lấy hoặc tạo Admin user làm CreatedByUserId
        var adminEmail = config["AdminSeed:Email"] ?? "admin@mms.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };
            await userManager.CreateAsync(adminUser, config["AdminSeed:Password"] ?? "Admin@123456");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        var adminId = adminUser.Id;

        // 2. Tiện nghi (Amenities)
        var amenityNames = new[]
        {
            "Điều hòa Inverter",
            "Bình nóng lạnh",
            "Tủ lạnh 2 cánh",
            "Máy giặt riêng",
            "WiFi cáp quang",
            "Bếp từ âm",
            "Ban công thoáng mát",
            "Tivi 43 inch",
            "Tủ quần áo âm tường",
            "Chỗ để xe trong nhà"
        };

        var amenities = amenityNames.Select(name => new Amenity(name)).ToList();
        await context.Amenities.AddRangeAsync(amenities, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} tiện nghi.", amenities.Count);

        // 3. Loại phòng (RoomTypes)
        var roomTypes = new List<RoomType>
        {
            new("Phòng đơn Standard", areaM2: 18m, maxOccupancy: 1, listPrice: 2_500_000m, "Phòng đơn khép kín, phù hợp 1 người ở."),
            new("Phòng đôi Superior", areaM2: 25m, maxOccupancy: 2, listPrice: 3_500_000m, "Phòng đôi rộng rãi, ban công thoáng mát."),
            new("Phòng Studio Deluxe", areaM2: 32m, maxOccupancy: 2, listPrice: 4_800_000m, "Studio full nội thất cao cấp, có bếp riêng."),
            new("Căn hộ mini 1PN", areaM2: 40m, maxOccupancy: 3, listPrice: 6_000_000m, "Căn hộ mini có phòng khách và phòng ngủ riêng biệt."),
            new("Căn hộ Duplex VIP", areaM2: 55m, maxOccupancy: 4, listPrice: 8_500_000m, "Căn hộ thông tầng gác lửng, thiết kế hiện đại, tiện nghi tối đa.")
        };
        await context.RoomTypes.AddRangeAsync(roomTypes, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} loại phòng.", roomTypes.Count);

        // 4. Người thuê (Tenants) bằng Bogus với Locale tiếng Việt
        Randomizer.Seed = new Random(123456); // Đảm bảo dữ liệu reproducible
        var fakerVi = new Faker("vi");

        var tenants = new List<Tenant>();
        for (int i = 0; i < 60; i++)
        {
            var fullName = fakerVi.Name.FullName();
            var phone = $"09{fakerVi.Random.Number(10000000, 99999999)}";
            tenants.Add(new Tenant(fullName, phone));
        }
        await context.Tenants.AddRangeAsync(tenants, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} người thuê.", tenants.Count);

        // 5. 50 Phòng (5 tầng, mỗi tầng 10 phòng: P101 -> P110, ..., P501 -> P510)
        var rooms = new List<Room>();
        var roomTypeLookup = roomTypes.ToDictionary(rt => rt.Id);

        for (int floor = 1; floor <= 5; floor++)
        {
            for (int roomIndex = 1; roomIndex <= 10; roomIndex++)
            {
                var roomCode = $"P{floor}{roomIndex:D2}";
                // Phân bổ loại phòng theo tầng & vị trí
                var type = (floor, roomIndex) switch
                {
                    (_, <= 4) => roomTypes[0], // Single
                    (_, <= 7) => roomTypes[1], // Double
                    (<= 3, _) => roomTypes[2], // Studio
                    (4, _) => roomTypes[3],    // 1PN
                    _ => roomTypes[4]          // Duplex VIP tầng 5
                };

                var room = new Room(roomCode, floor, type.Id);

                // Gán 3-6 tiện nghi ngẫu nhiên
                var selectedAmenities = fakerVi.PickRandom(amenities, fakerVi.Random.Int(3, 6));
                foreach (var amenity in selectedAmenities)
                {
                    room.AddAmenity(amenity.Id);
                }

                rooms.Add(room);
            }
        }
        await context.Rooms.AddRangeAsync(rooms, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} phòng.", rooms.Count);

        // 6. Vài nghìn hợp đồng không chồng lấn
        // Duyệt từng phòng, tạo chuỗi hợp đồng liên tiếp [StartDate, EndDate) từ 01/01/2022
        var today = clock.Today;
        var contractsToInsert = new List<Contract>();
        var handoversToInsert = new List<HandoverRecord>();

        // Giữ lại một số phòng trống (Available) hoặc Bảo trì (Maintenance) hiện tại để test nghiệp vụ
        var availableRoomCodes = new HashSet<string> { "P101", "P102", "P205", "P308", "P410" };
        var maintenanceRoomCodes = new HashSet<string> { "P103", "P510" };

        foreach (var room in rooms)
        {
            var rt = roomTypeLookup[room.RoomTypeId];
            var isAlwaysAvailableNow = availableRoomCodes.Contains(room.Code);
            var isMaintenanceNow = maintenanceRoomCodes.Contains(room.Code);

            // Bắt đầu từ 2022-01-01
            var cursorDate = new DateOnly(2022, 1, 1);
            var contractCountForRoom = fakerVi.Random.Int(35, 45); // ~40 hợp đồng/phòng * 50 = ~2,000 hợp đồng

            Contract? previousContract = null;

            for (int c = 0; c < contractCountForRoom; c++)
            {
                var durationMonths = fakerVi.Random.Int(1, 2); // 1-2 tháng mỗi hợp đồng để có mật độ dày
                var startDate = cursorDate;
                var endDate = startDate.AddMonths(durationMonths);

                // Nếu phòng này được quy định trống ở hiện tại, không tạo hợp đồng bao trùm ngày today
                if ((isAlwaysAvailableNow || isMaintenanceNow) && startDate <= today && today < endDate)
                {
                    // Đẩy mốc tiếp theo ra sau hôm nay 1 tháng để tạo khoảng trống hiện tại
                    cursorDate = today.AddDays(fakerVi.Random.Int(15, 30));
                    continue;
                }

                var period = new DateRange(startDate, endDate);
                var contract = new Contract(room.Id, period, renewedFromContractId: previousContract?.Id);

                // Chọn người thuê đại diện
                var primaryTenant = fakerVi.PickRandom(tenants);
                contract.AddTenant(primaryTenant.Id, isPrimary: true, rt.MaxOccupancy);

                // Nếu phòng có sức chứa > 1, có thể thêm người ở ghép
                if (rt.MaxOccupancy > 1 && fakerVi.Random.Bool(0.4f))
                {
                    var roommate = fakerVi.PickRandom(tenants);
                    if (roommate.Id != primaryTenant.Id)
                    {
                        contract.AddTenant(roommate.Id, isPrimary: false, rt.MaxOccupancy);
                    }
                }

                // Xử lý trạng thái theo mốc thời gian
                if (endDate <= today)
                {
                    // Hợp đồng quá khứ: Draft -> Active -> Expired
                    contract.Activate();
                    contract.TryExpire(today);

                    // Thêm biên bản bàn giao nhận & trả phòng
                    handoversToInsert.Add(new HandoverRecord(
                        contract.Id,
                        HandoverType.CheckIn,
                        "Phòng sạch sẽ, đầy đủ chìa khóa và trang thiết bị bàn giao ban đầu.",
                        keyCount: 2,
                        createdByUserId: adminId,
                        notes: "Nhận phòng đúng hẹn"));

                    handoversToInsert.Add(new HandoverRecord(
                        contract.Id,
                        HandoverType.CheckOut,
                        "Kiểm tra phòng nguyên vẹn, hao mòn tự nhiên theo thời gian, đã thu hồi đủ chìa khóa.",
                        keyCount: 2,
                        createdByUserId: adminId,
                        notes: "Hoàn tất thủ tục trả phòng"));
                }
                else if (startDate <= today && today < endDate)
                {
                    // Hợp đồng hiện tại: Draft -> Active
                    contract.Activate();
                    room.ChangeStatus(RoomStatus.Occupied);

                    handoversToInsert.Add(new HandoverRecord(
                        contract.Id,
                        HandoverType.CheckIn,
                        "Phòng bàn giao sạch sẽ, trang thiết bị hoạt động tốt.",
                        keyCount: 2,
                        createdByUserId: adminId,
                        notes: "Khách đang cư trú bình thường"));
                }
                else
                {
                    // Hợp đồng tương lai: giữ Draft (chờ check-in)
                }

                contractsToInsert.Add(contract);
                previousContract = contract;

                // cursorDate tiếp theo = endDate (nửa mở [Start, End), End kề Start -> KHÔNG BAO GIỜ CHỒNG LẤN)
                // Thi thoảng có khoảng trống 0 đến 5 ngày giữa các lần khách thuê
                var gapDays = fakerVi.Random.Bool(0.7f) ? 0 : fakerVi.Random.Int(1, 5);
                cursorDate = endDate.AddDays(gapDays);
            }

            if (isMaintenanceNow)
            {
                room.ChangeStatus(RoomStatus.Maintenance);
            }
        }

        // Lưu Contracts và HandoverRecords theo từng batch để tối ưu hiệu năng
        logger.LogInformation("Đang lưu {Count} hợp đồng không chồng lấn vào database...", contractsToInsert.Count);
        const int batchSize = 300;
        for (int i = 0; i < contractsToInsert.Count; i += batchSize)
        {
            var batch = contractsToInsert.Skip(i).Take(batchSize);
            await context.Contracts.AddRangeAsync(batch, ct);
            await context.SaveChangesAsync(ct);
        }
        logger.LogInformation("Đã lưu thành công {Count} hợp đồng.", contractsToInsert.Count);

        // Lưu HandoverRecords
        logger.LogInformation("Đang lưu {Count} biên bản bàn giao...", handoversToInsert.Count);
        for (int i = 0; i < handoversToInsert.Count; i += batchSize)
        {
            var batch = handoversToInsert.Skip(i).Take(batchSize);
            await context.HandoverRecords.AddRangeAsync(batch, ct);
            await context.SaveChangesAsync(ct);
        }

        // Cập nhật trạng thái các phòng (Occupied / Maintenance / Available)
        context.Rooms.UpdateRange(rooms);
        await context.SaveChangesAsync(ct);

        // 7. Giữ phòng (Reservations)
        var reservationsToInsert = new List<Reservation>();
        foreach (var roomCode in availableRoomCodes)
        {
            var r = rooms.First(x => x.Code == roomCode);
            var t = fakerVi.PickRandom(tenants);

            // Tạo đặt giữ phòng còn hiệu lực
            var activeRes = Reservation.Create(r.Id, t.Id, adminId, clock.UtcNow, TimeSpan.FromHours(48));
            if (fakerVi.Random.Bool(0.5f))
            {
                activeRes.Confirm();
            }
            r.ChangeStatus(RoomStatus.Reserved);
            reservationsToInsert.Add(activeRes);
        }
        await context.Reservations.AddRangeAsync(reservationsToInsert, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} giữ phòng hiệu lực.", reservationsToInsert.Count);

        // 8. Chỉ số điện nước (MeterReadings) cho các phòng trong 6 tháng gần nhất
        var meterReadingsToInsert = new List<MeterReading>();
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        foreach (var room in rooms)
        {
            decimal elec = fakerVi.Random.Decimal(100m, 500m);
            decimal water = fakerVi.Random.Decimal(20m, 60m);
            MeterReading? prevReading = null;

            for (int m = 6; m >= 1; m--)
            {
                var period = currentMonth.AddMonths(-m);
                elec += fakerVi.Random.Decimal(50m, 150m);
                water += fakerVi.Random.Decimal(5m, 15m);

                var mr = new MeterReading(room.Id, period, elec, water, prevReading);
                meterReadingsToInsert.Add(mr);
                prevReading = mr;
            }
        }
        await context.MeterReadings.AddRangeAsync(meterReadingsToInsert, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} bản ghi chỉ số điện nước.", meterReadingsToInsert.Count);

        // 9. Yêu cầu bảo trì (MaintenanceRequests)
        var maintenanceRequests = new List<MaintenanceRequest>();
        var sampleIssues = new[]
        {
            "Vòi nước bồn rửa mặt bị rò rỉ nước",
            "Điều hòa không mát, cần bảo dưỡng bơm gas",
            "Bóng đèn phòng tắm bị chập chờn",
            "Cửa sổ ban công bị kẹt khó đóng mở",
            "Áp lực nước nóng yếu vào buổi tối",
            "Ổ cắm điện góc bếp bị lỏng"
        };

        foreach (var sampleRoom in fakerVi.PickRandom(rooms, 25))
        {
            var desc = fakerVi.PickRandom(sampleIssues);
            var mr = new MaintenanceRequest(sampleRoom.Id, adminId, desc);

            var status = fakerVi.PickRandom(new[] { MaintenanceStatus.Open, MaintenanceStatus.InProgress, MaintenanceStatus.Done });
            if (status == MaintenanceStatus.InProgress)
            {
                mr.Start();
            }
            else if (status == MaintenanceStatus.Done)
            {
                mr.Start();
                mr.Complete();
            }

            maintenanceRequests.Add(mr);
        }
        await context.MaintenanceRequests.AddRangeAsync(maintenanceRequests, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Đã seed {Count} yêu cầu bảo trì.", maintenanceRequests.Count);

        logger.LogInformation("Hoàn tất khởi tạo toàn bộ dữ liệu mẫu Development thành công!");
    }
}
