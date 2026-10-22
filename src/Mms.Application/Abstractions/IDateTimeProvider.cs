namespace Mms.Application.Abstractions;

/// <summary>Tách thời gian ra khỏi DateTime.Now để unit test được các quy tắc theo ngày.</summary>
public interface IDateTimeProvider
{
    /// <summary>Thời điểm hiện tại (UTC), dùng cho các mốc sự kiện.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Ngày hôm nay theo múi giờ Việt Nam, dùng cho nghiệp vụ ngày lưu trú/hợp đồng.</summary>
    DateOnly Today { get; }
}
