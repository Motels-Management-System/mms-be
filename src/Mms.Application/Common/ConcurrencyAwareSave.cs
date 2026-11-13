using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;

namespace Mms.Application.Common;

/// <summary>
/// Lưu thay đổi của background job: bản ghi đã bị request khác sửa (xmin đổi) được nạp lại
/// từ DB để chu kỳ sau đánh giá lại, các bản ghi còn lại vẫn được lưu.
/// </summary>
public static class ConcurrencyAwareSave
{
    public static async Task RetryOnceAsync(IAppDbContext dbContext, CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
                await entry.ReloadAsync(ct);

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
