using Mms.Domain.Enums;

namespace Mms.Application.Features.Dashboard.Dtos;

/// <summary>Số phòng theo một trạng thái kèm tỷ trọng trong tổng số phòng.</summary>
public sealed record RoomStatusCountDto(RoomStatus Status, int Count, decimal Share);

/// <summary>Tóm tắt tình trạng phòng cho màn dashboard.</summary>
public sealed record DashboardSummaryDto(
    int TotalRooms,
    IReadOnlyList<RoomStatusCountDto> ByStatus);
