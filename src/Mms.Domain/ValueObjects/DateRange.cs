using Mms.Domain.Exceptions;

namespace Mms.Domain.ValueObjects;

/// <summary>
/// Khoảng ngày nửa mở [Start, End): End là ngày phòng sẵn sàng cho người khác,
/// nên hợp đồng A kết thúc 01/03 và hợp đồng B bắt đầu 01/03 không bị coi là chồng lấn.
/// </summary>
public readonly record struct DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end <= start)
            throw new DomainException("Ngày kết thúc phải sau ngày bắt đầu.");

        Start = start;
        End = end;
    }

    /// <summary>Cộng tháng bằng DateOnly.AddMonths: 31/01 + 1 tháng = 28/02 (hoặc 29/02 năm nhuận).</summary>
    public static DateRange FromMonths(DateOnly start, int months)
    {
        if (months < 1)
            throw new DomainException("Thời hạn thuê tối thiểu là 1 tháng.");

        return new DateRange(start, start.AddMonths(months));
    }

    public bool Overlaps(DateRange other) => Start < other.End && other.Start < End;

    public bool Contains(DateOnly date) => date >= Start && date < End;

    /// <summary>Số ngày còn lại tính đến ngày kết thúc (âm nếu đã quá hạn).</summary>
    public int DaysUntilEnd(DateOnly today) => End.DayNumber - today.DayNumber;
}
