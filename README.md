# mms-be
motels management system apis

## Kiểm thử

| Bộ test | Kết quả |
|---|---|
| `Mms.Domain.UnitTests` | 59 passed |
| `Mms.Application.UnitTests` | 40 passed |
| `Mms.Api.IntegrationTests` (Testcontainers PostgreSQL thật) | 57 passed |

### Test concurrency 50 request song song

`CreateContract_50ParallelRequestsForSameRoomSamePeriod_ExactlyOne201AndFortyNine409`
trong `tests/Mms.Api.IntegrationTests/ContractEndpointsTests.cs`:

- 50 request HTTP đồng thời tạo hợp đồng cho **cùng một phòng, cùng khoảng ngày** `[from, from + 3 tháng)`.
- Kết quả: **đúng 1 request `201 Created` và 49 request `409 Conflict`** (`code: ROOM_DATE_OVERLAP`).
- Khẳng định bằng cả 3 lớp: status code từng response, mã lỗi `ROOM_DATE_OVERLAP` (từ middleware bắt
  Postgres `23P01` hoặc `RoomDateOverlapException` của handler), và đếm trong DB chỉ còn 1 hợp đồng.
- Cơ chế bảo vệ: exclusion constraint `EX_Contracts_NoOverlap` (`EXCLUDE USING gist` trên `RoomId` + daterange
  `[StartDate, EndDate)`), ngoài kiểm tra thân thiện ở handler. Kiểm tra ở handler là để trả thông báo
  rõ ràng; constraint mới là lớp bảo vệ thật sự khi các request cùng lúc vượt qua kiểm tra đó.
