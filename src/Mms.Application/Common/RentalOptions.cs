namespace Mms.Application.Common;

public sealed class RentalOptions
{
    public const string SectionName = "RentalOptions";

    /// <summary>Thời gian giữ phòng trước khi tự hết hạn (giờ).</summary>
    public int ReservationHoldHours { get; set; } = 48;

    /// <summary>Thời hạn hợp đồng tối thiểu (tháng).</summary>
    public int MinContractMonths { get; set; } = 1;

    /// <summary>Số ngày báo trước khi chấm dứt sớm.</summary>
    public int EarlyTerminationNoticeDays { get; set; } = 30;
}
