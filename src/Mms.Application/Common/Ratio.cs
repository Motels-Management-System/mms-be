namespace Mms.Application.Common;

/// <summary>Tỷ lệ 0..1 làm tròn 4 chữ số, mặc định 0 khi mẫu số bằng 0 (chưa có dữ liệu).</summary>
public static class Ratio
{
    public static decimal Of(long part, long total)
        => total <= 0 ? 0m : Math.Round((decimal)part / total, 4, MidpointRounding.AwayFromZero);
}
