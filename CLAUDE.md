# CLAUDE.md: Motel Management System (mms-be)

> Đọc toàn bộ file này trước khi viết code. Làm **một tính năng mỗi lần** theo "Quy trình" ở mục 4, và tick `[x]` ở mục 6 khi xong. Việc nào chưa rõ quy tắc nghiệp vụ thì hỏi người dùng, đừng tự đoán.

## 1. Tổng quan

Hệ thống quản lý khoảng 100 phòng trọ **cho thuê theo tháng** (không phải theo đêm). Người dùng tìm phòng trống và giữ phòng; nhân viên quản lý phòng, hợp đồng, nhận/trả phòng; chủ trọ xem dashboard và báo cáo.

Đây là dự án portfolio cho vị trí **fresher backend .NET**, nên ưu tiên chất lượng backend: nghiệp vụ ngày tháng, truy vấn dữ liệu, concurrency, quy tắc hợp đồng. Frontend giữ đơn giản.

**Vai trò:** `Admin` (chủ trọ), `Staff` (nhân viên), `Customer` (người thuê có tài khoản), `Anonymous` (khách chưa đăng nhập, chỉ xem và tìm phòng).

**Ngoài phạm vi, TUYỆT ĐỐI không làm** (chính sách thuế chưa rõ): hóa đơn, thanh toán, đặt cọc, hoàn tiền, khuyến mãi, giá theo mùa, cổng thanh toán, SignalR, đa ngôn ngữ. `RoomType.ListPrice` chỉ để hiển thị. Không tạo bảng hay endpoint liên quan tiền.

## 2. Công nghệ

| Nhóm | Công nghệ |
|---|---|
| Runtime | .NET 9, ASP.NET Core Web API (controllers) |
| ORM / DB | EF Core 9.0.x + Npgsql 9.0.x, PostgreSQL 18 (máy local), tên DB `MotelManagementSystem` |
| Xác thực | ASP.NET Identity (`ApplicationUser : IdentityUser<Guid>`) + JWT Bearer + refresh token |
| Validation | FluentValidation |
| Log / tài liệu API | Serilog, Swagger/OpenAPI |
| Test | xUnit, FluentAssertions **7.x** (không nâng lên 8+, khác giấy phép), NSubstitute, Testcontainers.PostgreSql, Bogus |
| Công cụ | `dotnet-ef` 9.0.4 cài dạng **local tool** (`.config/dotnet-tools.json`), Docker (tùy chọn) |
| Frontend | Vite + React + TypeScript, Ant Design, Axios, React Router, dayjs, Recharts (repo riêng, dự kiến `mms-fe`) |

**Không dùng:** MediatR (chưa cài, tự viết handler thường), AutoMapper (map tay hoặc Mapster nếu cần), Dapper. **Không nâng package EF Core/Npgsql/ASP.NET lên 10.x**, giữ dòng 9.x.

## 3. Cấu trúc và quy tắc kiến trúc

```text
src/
  Mms.Domain/          Entity, Enum, ValueObject (DateRange), DomainException. Không phụ thuộc package nào.
  Mms.Application/     Use case, DTO, Validator, interface (IAppDbContext, IDateTimeProvider, IReportQueries...)
  Mms.Infrastructure/  AppDbContext, Configurations, Migrations, Identity, BackgroundJobs, Services, Reports (SQL thuần)
  Mms.Api/             Controllers, Middleware, Program.cs
tests/
  Mms.Domain.UnitTests / Mms.Application.UnitTests / Mms.Api.IntegrationTests
```

**Hướng phụ thuộc (không được vi phạm):** `Api → Application, Infrastructure`; `Infrastructure → Application → Domain`. Domain không tham chiếu gì. Application không tham chiếu Infrastructure hay Api. Controller không dùng `AppDbContext` trực tiếp, chỉ gọi handler.

**Trạng thái hiện tại (đã xong):** cấu trúc solution, toàn bộ entity Domain, `AppDbContext` + configurations, 2 migration (`Initial`, `AddNoOverlapConstraint`), `AddApplication()` (mới đăng ký validator), `AddInfrastructure(connectionString)` đã nối vào `Program.cs`.

## 4. Quy trình làm một tính năng

1. **Đọc** entity liên quan trong Domain và mục 5. Nếu quy tắc nghiệp vụ chưa có trong Domain thì thêm method vào entity (không viết logic nghiệp vụ trong controller hay handler).
2. **Domain:** thêm/sửa method + unit test trong `Mms.Domain.UnitTests` (đặc biệt các ca biên về ngày tháng).
3. **Application:** tạo theo thư mục `Features/<Module>/`: `Commands/` hoặc `Queries/` (record + handler), `Dtos/`, `Validators/`. Handler gọi validator trước tiên, dùng `IAppDbContext` và `IDateTimeProvider`.
4. **Infrastructure:** chỉ khi đổi schema hoặc cần SQL thuần. Tạo migration (mục 9), kiểm tra file migration sinh ra trước khi chạy `database update`.
5. **Api:** thêm endpoint vào controller, gắn `[Authorize(Roles = ...)]`, trả đúng status code (mục 5.3).
6. **Test:** unit test cho handler có logic; integration test (Testcontainers) cho truy vấn, constraint và concurrency.
7. **Kiểm tra:** `dotnet build` không lỗi, `dotnet test` xanh, không để warning mới.
8. **Cập nhật** checkbox ở mục 6/7 và commit (Conventional Commits: `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`).

## 5. Quy ước code

### 5.1 Domain
- Entity có setter `private`, tạo qua constructor/factory, đổi trạng thái qua method (`Confirm()`, `Activate()`, `Terminate()`...). Vi phạm quy tắc thì ném `DomainException` với thông báo tiếng Việt.
- Id là Guid v7 (`BaseEntity`). `AuditableEntity` có `CreatedAt/UpdatedAt` do `AppDbContext.SaveChangesAsync` gán.
- `uint Version` trên `Room`, `Contract`, `Reservation` ánh xạ sang cột hệ thống `xmin` (optimistic concurrency). Migration chỉ hiện dòng `xmin ... rowVersion: true`, đó là đúng.
- Trạng thái lưu dưới dạng **string** trong DB (`HasConversion<string>()`).

### 5.2 Ngày tháng (điểm nhấn của dự án)
- Ngày hợp đồng dùng `DateOnly`; mốc sự kiện dùng `DateTimeOffset` UTC.
- **Không bao giờ** gọi `DateTime.Now`, `DateTime.UtcNow`, `DateOnly.FromDateTime(...)` trực tiếp. Luôn dùng `IDateTimeProvider` (`UtcNow`, `Today` theo múi giờ Việt Nam) để test được.
- Khoảng thuê là nửa mở `[StartDate, EndDate)`: `EndDate` là ngày phòng sẵn sàng cho người khác. Dùng `DateRange` và `DateOnly.AddMonths` (31/01 + 1 tháng = 28/02). Hai hợp đồng kề nhau (A kết thúc 01/03, B bắt đầu 01/03) là hợp lệ.
- "Sắp hết hạn" là **giá trị tính toán** (`DaysRemaining`, `ExpiryLevel`), không lưu cột. Mức: `Red` khi còn ≤ 7 ngày, `Yellow` khi ≤ 30 ngày, ngược lại `None`.

### 5.3 API
- Controller mỏng: nhận request → gọi handler → trả DTO. **Không trả entity** ra ngoài.
- Route dạng `/api/<resource>` (số nhiều, kebab-case). JSON camelCase. `DateOnly` dạng `yyyy-MM-dd`, thời điểm dạng ISO 8601 UTC.
- Danh sách luôn phân trang: query `page`, `pageSize` (mặc định 1 và 20, tối đa 100), trả `PagedResult<T>` (`items`, `page`, `pageSize`, `totalCount`).
- Lỗi trả `ProblemDetails` (RFC 7807) có thêm trường `code` ổn định để frontend xử lý. Middleware xử lý lỗi toàn cục (`Api/Middleware`) ánh xạ:

| Nguồn lỗi | Status | `code` ví dụ |
|---|---|---|
| FluentValidation | 400 | `VALIDATION_ERROR` |
| `DomainException` | 422 | `BUSINESS_RULE_VIOLATION` |
| `NotFoundException` | 404 | `NOT_FOUND` |
| `ConflictException`, Postgres `23505` (trùng unique) | 409 | `DUPLICATE` |
| Postgres `23P01` (exclusion constraint) | 409 | `ROOM_DATE_OVERLAP` |
| `DbUpdateConcurrencyException` | 409 | `CONCURRENCY_CONFLICT` |
| Chưa đăng nhập / sai quyền | 401 / 403 | (mặc định) |

- Mã trạng thái thành công: `201` + `Location` khi tạo, `200` khi đọc/sửa, `204` khi xóa/hành động không trả dữ liệu.

### 5.4 Application
- Handler là class thường, **không dùng MediatR**. Tạo interface trong `Application/Abstractions/Messaging`:
  `ICommandHandler<TCommand, TResult>` và `IQueryHandler<TQuery, TResult>` với `Task<TResult> HandleAsync(TRequest, CancellationToken)`. Đăng ký tự động bằng quét assembly trong `AddApplication()` (viết khi làm use case đầu tiên). Controller inject trực tiếp handler cần dùng.
- Mỗi command có `AbstractValidator<T>`; handler gọi `await validator.ValidateAndThrowAsync(command, ct)` đầu tiên.
- Truy vấn đọc dùng `AsNoTracking()` và `Select` thẳng sang DTO (không `Include` rồi map thủ công).
- Truy vấn báo cáo phức tạp (`generate_series`, window function) viết SQL thuần trong `Infrastructure/Reports` bằng `Database.SqlQuery<T>`, expose qua interface `IReportQueries` ở Application.
- Cấu hình nghiệp vụ lấy từ `IOptions<RentalOptions>` (mục 8), không hardcode.

### 5.5 Dữ liệu và EF Core
- **Không sửa migration đã áp dụng.** Muốn đổi schema thì tạo migration mới.
- Exclusion constraint `EX_Contracts_NoOverlap` được thêm bằng SQL thủ công trong migration (EF không có API). **Không có `WHERE` theo trạng thái**: mọi hợp đồng đều chiếm `[StartDate, EndDate)`. Hệ quả: hợp đồng Draft bị bỏ dở phải **xóa**; chấm dứt sớm thì `EndDate` được rút ngắn (đã làm trong `Contract.Terminate`).
- Mỗi phòng chỉ có tối đa một giữ phòng còn hiệu lực: unique index có điều kiện `UX_Reservations_ActiveHold_Room`.
- Chống trùng nhiều lớp: (1) kiểm tra ở handler để trả thông báo thân thiện, (2) exclusion constraint là lớp bảo vệ thật sự, (3) optimistic concurrency bằng `xmin`, (4) middleware bắt `23P01` → 409.
- Thao tác nhiều bảng (ví dụ tạo hợp đồng + chuyển giữ phòng sang `ConvertedToContract`) phải nằm trong **một** lần `SaveChangesAsync` hoặc một transaction.

### 5.6 Test
- Domain: unit test thuần, không mock. Handler: NSubstitute cho `IDateTimeProvider`, dùng DbContext thật qua Testcontainers khi cần truy vấn.
- Integration test dùng `WebApplicationFactory` + Testcontainers PostgreSQL (`postgres:16-alpine`), apply migration thật (để constraint tồn tại).
- **Bắt buộc có:** test 50 request song song tạo hợp đồng cùng một phòng cùng khoảng ngày, kết quả đúng 1 `201` và 49 `409`. Ghi kết quả vào README.
- Tên test: `Method_Scenario_ExpectedResult`. Dùng FluentAssertions 7.x.

### 5.7 Quy ước chung
- Định danh (class, method, route) bằng **tiếng Anh**. Comment và thông báo lỗi hiển thị cho người dùng bằng **tiếng Việt**.
- Không commit bí mật. Chuỗi kết nối thật nằm trong `dotnet user-secrets`; `appsettings*.json` chỉ chứa placeholder.
- Không thêm package mới mà không báo lý do. Không bỏ qua warning bằng `#pragma` trừ khi có comment giải thích.
- Giữ code đơn giản, không thêm abstraction thừa (không generic repository, không Unit of Work riêng vì `DbContext` đã là UoW).

## 6. Danh sách chức năng backend (API)

Ký hiệu quyền: `A` = Admin, `S` = Staff, `C` = Customer, `*` = ai cũng gọi được (kể cả chưa đăng nhập). Admin luôn có quyền của Staff.

### Giai đoạn 0: Nền tảng (đã xong)
- [x] Cấu trúc solution Clean Architecture, 7 project build xanh
- [x] Domain: entity, enum, `DateRange`, quy tắc trạng thái
- [x] `AppDbContext`, configurations, migration `Initial` + `AddNoOverlapConstraint`
- [x] Test thủ công exclusion constraint trong pgAdmin

### Giai đoạn 1: Hạ tầng API và xác thực
- [x] Global exception handler → `ProblemDetails` theo bảng mục 5.3
- [x] Serilog (console + file), Swagger UI có nút Authorize (Bearer)
- [x] `PagedResult<T>`, `NotFoundException`, `ConflictException`, `RentalOptions`
- [x] Seed vai trò `Admin`, `Staff`, `Customer` và tài khoản Admin mặc định (đọc từ cấu hình)
- [x] `POST /api/auth/register` (`*`): đăng ký Customer, tự tạo hồ sơ `Tenant` liên kết `UserId`
- [x] `POST /api/auth/login` (`*`): trả access token (JWT) + refresh token
- [x] `POST /api/auth/refresh` (`*`), `POST /api/auth/logout` (đăng nhập)
- [x] `GET /api/auth/me`, `PUT /api/auth/change-password` (đăng nhập)
- [x] Entity + bảng `RefreshTokens` (migration mới)
- [x] `GET /api/users` (A), `POST /api/users` (A, tạo Staff), `PUT /api/users/{id}/roles` (A), `PATCH /api/users/{id}/active` (A, khóa/mở)

### Giai đoạn 2: Danh mục
- [x] Loại phòng: `GET /api/room-types` (`*`), `GET /api/room-types/{id}` (`*`), `POST`, `PUT /{id}`, `DELETE /{id}` (A; không xóa nếu còn phòng thuộc loại)
- [x] Tiện nghi: `GET /api/amenities` (`*`), `POST`, `PUT /{id}`, `DELETE /{id}` (A)
- [x] Phòng: `GET /api/rooms` (`*`; lọc `status`, `roomTypeId`, `floor`, `keyword`, phân trang)
- [x] `GET /api/rooms/{id}` (`*`): kèm loại phòng, tiện nghi; S/A thấy thêm hợp đồng hiện tại
- [x] `POST /api/rooms`, `PUT /api/rooms/{id}` (A), `PUT /api/rooms/{id}/amenities` (A)
- [x] `POST /api/rooms/{id}/status` (S): chuyển `Maintenance` ⇄ `Available` theo `Room.ChangeStatus`
- [x] Người thuê: `GET /api/tenants` (S; tìm theo tên/SĐT, phân trang), `GET /api/tenants/{id}` (S), `POST`, `PUT /{id}` (S), `GET /api/tenants/{id}/contracts` (S, lịch sử thuê)

### Giai đoạn 3: Lõi nghiệp vụ (ghi điểm CV)
- [ ] **Tìm phòng trống** `GET /api/rooms/available?from=&to=&guests=&roomTypeId=&amenityIds=` (`*`). Phòng khả dụng khi: `Status != Maintenance`, không có hợp đồng chồng `[from, to)` (`NOT EXISTS`), không có giữ phòng còn hiệu lực, `MaxOccupancy >= guests`. Có index hỗ trợ, đo `EXPLAIN ANALYZE`
- [ ] Giữ phòng: `POST /api/reservations` (C, S): kiểm tra phòng còn trống, tạo `Pending` hết hạn sau `ReservationHoldHours`, bắt `23505` khi trùng giữ phòng
- [ ] `GET /api/reservations` (S; lọc `status`), `GET /api/reservations/me` (C), `GET /api/reservations/{id}` (S hoặc chủ sở hữu)
- [ ] `POST /api/reservations/{id}/confirm` (S): phòng → `Reserved`
- [ ] `POST /api/reservations/{id}/cancel` (S hoặc chủ sở hữu): phòng → `Available`
- [ ] **Tạo hợp đồng** `POST /api/contracts` (S): nhận `roomId`, `startDate`, `months`, `tenants[]` (có đúng 1 người đại diện), `reservationId?`. Kiểm tra phòng không bảo trì, số người ≤ sức chứa, `months ≥ MinContractMonths`; chuyển giữ phòng thành `ConvertedToContract` trong cùng transaction; bắt `23P01` → 409
- [ ] `GET /api/contracts` (S; lọc `status`, `roomId`, `expiringWithinDays`), `GET /api/contracts/{id}` (S, hoặc C nếu là người ở)
- [ ] `GET /api/contracts/me` (C): hợp đồng của mình kèm `daysRemaining`, `expiryLevel`
- [ ] `POST /api/contracts/{id}/tenants` (S), `DELETE /api/contracts/{id}/tenants/{tenantId}` (S; cần thêm `Contract.RemoveTenant` vào Domain)
- [ ] **Check-in** `POST /api/contracts/{id}/check-in` (S): chỉ khi hôm nay ≥ `StartDate` và phòng `Available`/`Reserved`; `Activate()`, phòng → `Occupied`, ghi biên bản bàn giao
- [ ] **Gia hạn** `POST /api/contracts/{id}/renew` (S): tạo hợp đồng Draft nối tiếp đúng `EndDate` (`Contract.CreateRenewal`); cần thêm cột `RenewedFromContractId` (nullable) vào `Contract`
- [ ] **Chấm dứt sớm** `POST /api/contracts/{id}/terminate` (S): `terminationDate ≥ hôm nay + EarlyTerminationNoticeDays`
- [ ] **Check-out** `POST /api/contracts/{id}/check-out` (S): hợp đồng phải đã `Expired`/`Terminated`; ghi biên bản (tình trạng phòng, số chìa khóa, ghi chú); phòng → `Available` hoặc `Maintenance`
- [ ] **Đổi phòng** `POST /api/contracts/{id}/change-room` (S): phòng mới phải trống trong toàn bộ thời gian còn lại
- [ ] Entity + bảng `HandoverRecords` (`ContractId`, `Type` CheckIn/CheckOut, `RoomCondition`, `KeyCount`, `Notes`, `CreatedByUserId`)
- [ ] **Integration test 50 request song song** tạo hợp đồng cùng phòng → 1 thành công, 49 `409`
- [ ] Unit test các ca biên: cộng tháng 29/30/31, năm nhuận, gia hạn nối tiếp, kề nhau không chồng lấn

### Giai đoạn 4: Vận hành
- [ ] Chỉ số điện nước: `POST /api/rooms/{roomId}/meter-readings` (S; chỉ số mới ≥ cũ trừ khi thay đồng hồ), `GET /api/rooms/{roomId}/meter-readings` (S)
- [ ] `GET /api/meter-readings/missing?month=yyyy-MM` (S): các phòng đang có người ở nhưng chưa ghi chỉ số tháng đó
- [ ] Bảo trì: `POST /api/maintenance-requests` (C với phòng mình đang thuê, S), `GET /api/maintenance-requests` (S; lọc `status`, `roomId`), `GET /api/maintenance-requests/me` (C)
- [ ] `POST /api/maintenance-requests/{id}/start`, `POST /api/maintenance-requests/{id}/complete` (S)
- [ ] Job `ExpireReservationsJob` (`BackgroundService`, chạy mỗi phút): `Reservation.TryExpire`, trả phòng về `Available`
- [ ] Job `ExpireContractsJob` (chạy mỗi ngày 00:05 giờ VN): `Contract.TryExpire`; nếu có hợp đồng gia hạn Draft bắt đầu đúng ngày đó thì tự `Activate` (phòng giữ `Occupied`, không check-out); nếu không thì chờ check-out
- [ ] `AuditSaveChangesInterceptor`: ghi `AuditLog` (ai, lúc nào, giá trị cũ/mới) cho Room, Contract, Reservation, Tenant, MaintenanceRequest
- [ ] `GET /api/audit-logs` (A; lọc `entityName`, `entityId`, `userId`, khoảng ngày)

### Giai đoạn 5: Dashboard và báo cáo
- [ ] `GET /api/dashboard/summary` (S): số phòng theo trạng thái
- [ ] `GET /api/dashboard/rooms` (S): lưới phòng kèm người thuê hiện tại, `daysRemaining`, `expiryLevel`
- [ ] `GET /api/dashboard/expiring-contracts?days=30` (S): hợp đồng sắp hết hạn, sắp theo ngày còn lại
- [ ] `GET /api/reports/occupancy?from=&to=&groupBy=month|roomType` (A): tỷ lệ lấp đầy = số ngày-phòng có người ở / (số phòng × số ngày); dùng `generate_series`
- [ ] `GET /api/reports/vacancy-duration?from=&to=` (A): số ngày trống trung bình giữa 2 hợp đồng liên tiếp của một phòng (window function `LEAD`)
- [ ] `GET /api/reports/forecast?days=30|60|90` (A): phòng sẽ trống trong N ngày tới (hợp đồng kết thúc, chưa có gia hạn)
- [ ] `GET /api/reports/renewal-rate?from=&to=` (A): tỷ lệ gia hạn và tỷ lệ quay vòng (turnover)

### Giai đoạn 6: Chất lượng và đóng gói
- [ ] Seed dữ liệu mẫu bằng Bogus (chỉ môi trường Development): ~100 phòng, vài loại phòng, tiện nghi, vài nghìn hợp đồng không chồng lấn
- [ ] Đo `EXPLAIN ANALYZE` trước/sau khi thêm index, ghi số liệu vào README
- [ ] Health check `/health` (kiểm tra kết nối DB)
- [ ] `Dockerfile` cho API, mở rộng `docker-compose.yml` chạy cả API + PostgreSQL
- [ ] GitHub Actions: build + test mỗi lần push
- [ ] README: ERD, sơ đồ kiến trúc, GIF demo, mục "quyết định kỹ thuật và vì sao"
- [ ] Deploy bản demo (Render/Railway/Fly.io)
- [ ] (Tùy chọn, sau cùng) email xác nhận bằng MailKit, ảnh phòng, xuất Excel báo cáo

## 7. Màn hình frontend

Frontend ở repo riêng, đơn giản, ưu tiên đủ để demo. Dùng Ant Design (Table, Form, DatePicker, Modal, Tag, Statistic). Tag trạng thái phòng: `Available` xanh lá, `Reserved` vàng, `Occupied` xanh dương, `Maintenance` xám. Countdown hợp đồng: `Yellow` ≤ 30 ngày, `Red` ≤ 7 ngày (lấy `expiryLevel` từ API, không tự tính trên client). Interceptor Axios: gắn Bearer token, tự refresh khi gặp 401, hiển thị `ProblemDetails.title` bằng `message.error`, form hiển thị lỗi theo `code`.

**Dùng chung**
- [ ] Layout có sidebar theo vai trò, route guard theo vai trò, trang 403/404

**Công khai và Customer**
- [ ] `/login`, `/register`
- [ ] `/rooms`: danh sách và **tìm phòng trống** (chọn ngày, số khách, loại phòng, tiện nghi) → `GET /api/rooms/available`
- [ ] `/rooms/:id`: chi tiết phòng, tiện nghi, nút "Giữ phòng" (yêu cầu đăng nhập)
- [ ] `/my/reservations`: giữ phòng của tôi, hủy giữ phòng, đếm ngược thời hạn giữ
- [ ] `/my/contract`: hợp đồng của tôi kèm đồng hồ đếm ngược số ngày còn lại
- [ ] `/my/maintenance`: gửi và theo dõi yêu cầu sửa chữa
- [ ] `/profile`: thông tin cá nhân, đổi mật khẩu

**Staff**
- [ ] `/dashboard`: thẻ thống kê theo trạng thái, lưới phòng có màu trạng thái và đồng hồ đếm ngược, bảng hợp đồng sắp hết hạn
- [ ] `/staff/rooms`: bảng phòng có bộ lọc; `/staff/rooms/:id` gồm thông tin, hợp đồng hiện tại, lịch sử, chỉ số điện nước, yêu cầu bảo trì, nút chuyển bảo trì
- [ ] `/staff/reservations`: bảng giữ phòng, nút xác nhận và hủy
- [ ] `/staff/contracts`: bảng hợp đồng có bộ lọc (trạng thái, sắp hết hạn)
- [ ] `/staff/contracts/new`: tạo hợp đồng theo bước (chọn phòng và ngày → người ở → xác nhận), hiển thị lỗi 409 trùng ngày dễ hiểu
- [ ] `/staff/contracts/:id`: chi tiết và các hành động check-in, gia hạn, chấm dứt sớm, check-out, đổi phòng, thêm/bớt người ở
- [ ] `/staff/tenants`, `/staff/tenants/:id`: tìm, thêm, sửa, lịch sử thuê
- [ ] `/staff/meter-readings`: bảng nhập chỉ số điện nước theo tháng, đánh dấu phòng chưa nhập
- [ ] `/staff/maintenance`: bảng (hoặc kanban) Open / InProgress / Done

**Admin**
- [ ] `/admin/reports`: biểu đồ tỷ lệ lấp đầy, thời gian trống trung bình, dự báo 30/60/90 ngày, tỷ lệ gia hạn (Recharts)
- [ ] `/admin/users`: quản lý tài khoản và vai trò
- [ ] `/admin/room-types`, `/admin/amenities`, `/admin/rooms`: CRUD danh mục
- [ ] `/admin/audit-logs`: tra cứu nhật ký thay đổi

## 8. Giá trị mặc định đang dùng (có thể đổi, cấu hình qua `RentalOptions` trong appsettings)

| Tham số | Mặc định | Ý nghĩa |
|---|---|---|
| `ReservationHoldHours` | 48 | Thời gian giữ phòng trước khi tự hết hạn |
| `MinContractMonths` | 1 | Thời hạn hợp đồng tối thiểu |
| `EarlyTerminationNoticeDays` | 30 | Số ngày báo trước khi chấm dứt sớm |

Quy ước đã chốt: Customer chỉ **giữ phòng**; **Staff mới tạo hợp đồng**. Khoảng thuê là nửa mở `[Start, End)`.

## 9. Lệnh hay dùng (PowerShell, chạy ở thư mục gốc `mms-be`)

```powershell
dotnet build
dotnet test
dotnet run --project src/Mms.Api

# Migration (dùng dotnet-ef local tool 9.0.4)
dotnet ef migrations add <TenMigration> -p src/Mms.Infrastructure -s src/Mms.Api -o Persistence/Migrations
dotnet ef database update -p src/Mms.Infrastructure -s src/Mms.Api
dotnet ef migrations remove -p src/Mms.Infrastructure -s src/Mms.Api   # chỉ khi migration CHƯA áp dụng

# Chuỗi kết nối thật (không commit)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=MotelManagementSystem;Username=postgres;Password=..." --project src/Mms.Api
```

Integration test dùng Testcontainers, cần Docker daemon đang chạy. Testcontainers 4.16 gọi Docker API 1.44; nếu engine cũ (Docker Desktop 20.10 chỉ hỗ trợ 1.41) thì đặt `DOCKER_API_VERSION=1.41` khi chạy `dotnet test`.

Cảnh báo "EF tools version 9.0.4 is older than the runtime 9.0.x" là bình thường, bỏ qua.
