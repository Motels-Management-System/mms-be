namespace Mms.Application.Common;

/// <summary>
/// Trùng lịch phòng: khoảng thuê [StartDate, EndDate) chồng với hợp đồng khác.
/// Cùng code lỗi ROOM_DATE_OVERLAP với exclusion constraint 23P01 để frontend xử lý đồng nhất.
/// </summary>
public class RoomDateOverlapException(string message)
    : ConflictException(message);
