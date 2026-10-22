using Mms.Domain.Common;
using Mms.Domain.Exceptions;

namespace Mms.Domain.Entities;

/// <summary>Chỉ số điện nước theo tháng. Chỉ ghi nhận, chưa tính tiền.</summary>
public class MeterReading : AuditableEntity
{
    private MeterReading() { }

    public MeterReading(
        Guid roomId, DateOnly periodMonth, decimal electricity, decimal water,
        MeterReading? previous = null, bool isMeterReplaced = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(electricity);
        ArgumentOutOfRangeException.ThrowIfNegative(water);

        var month = new DateOnly(periodMonth.Year, periodMonth.Month, 1);

        if (previous is not null)
        {
            if (previous.RoomId != roomId)
                throw new DomainException("Chỉ số kỳ trước không thuộc phòng này.");

            if (previous.PeriodMonth >= month)
                throw new DomainException("Kỳ ghi chỉ số phải sau kỳ trước.");

            if (!isMeterReplaced && (electricity < previous.Electricity || water < previous.Water))
                throw new DomainException("Chỉ số mới không được nhỏ hơn chỉ số kỳ trước (trừ khi thay đồng hồ).");
        }

        RoomId = roomId;
        PeriodMonth = month;
        Electricity = electricity;
        Water = water;
        IsMeterReplaced = isMeterReplaced;
    }

    public Guid RoomId { get; private set; }

    /// <summary>Ngày đầu tháng của kỳ ghi.</summary>
    public DateOnly PeriodMonth { get; private set; }

    public decimal Electricity { get; private set; }
    public decimal Water { get; private set; }
    public bool IsMeterReplaced { get; private set; }
}
