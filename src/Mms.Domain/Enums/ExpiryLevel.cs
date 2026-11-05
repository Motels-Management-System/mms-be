namespace Mms.Domain.Enums;

/// <summary>Mức cảnh báo sắp hết hạn (giá trị tính toán, không lưu DB): Red ≤ 7 ngày, Yellow ≤ 30 ngày.</summary>
public enum ExpiryLevel
{
    None,
    Yellow,
    Red
}
