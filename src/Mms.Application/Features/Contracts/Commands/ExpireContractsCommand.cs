namespace Mms.Application.Features.Contracts.Commands;

/// <summary>
/// Đánh dấu hết hạn hợp đồng Active đã tới EndDate; nếu có hợp đồng gia hạn Draft bắt đầu
/// đúng ngày đó thì kích hoạt để phòng không bị gián đoạn. Lệnh nội bộ, không có dữ liệu người dùng.
/// </summary>
public sealed record ExpireContractsCommand;
