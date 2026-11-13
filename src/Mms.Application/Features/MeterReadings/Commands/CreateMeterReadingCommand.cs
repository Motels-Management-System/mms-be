namespace Mms.Application.Features.MeterReadings.Commands;

/// <summary>
/// Ghi chỉ số điện nước cho một phòng trong một kỳ tháng.
/// Chỉ số nhỏ hơn kỳ trước chỉ hợp lệ khi đồng hồ đã được thay (IsMeterReplaced).
/// </summary>
public sealed record CreateMeterReadingCommand(
    Guid RoomId,
    DateOnly PeriodMonth,
    decimal Electricity,
    decimal Water,
    bool IsMeterReplaced = false);
