using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Features.Contracts.Commands;
using Mms.Infrastructure.Services;

namespace Mms.Infrastructure.BackgroundJobs;

/// <summary>
/// Chạy lúc 00:05 giờ Việt Nam mỗi ngày: hợp đồng tới EndDate chuyển Expired,
/// hợp đồng gia hạn Draft nối tiếp được kích hoạt.
/// Job cũng chạy ngay khi API khởi động để hợp đồng hết hạn trong lúc tắt máy không bị bỏ sót.
/// </summary>
public sealed class ContractExpiryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ContractExpiryJob> logger) : BackgroundService
{
    private static readonly TimeOnly ScheduleTime = new(0, 5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(DelayUntilNextRun(), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunSafelyAsync(stoppingToken);
        }
    }

    private async Task RunSafelyAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ExpireContractsCommand, int>>();

            var expired = await handler.HandleAsync(new ExpireContractsCommand(), ct);

            if (expired > 0)
                logger.LogInformation("Đã chuyển {Count} hợp đồng sang Expired.", expired);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không hết hạn được hợp đồng, sẽ thử lại vào lịch ngày sau.");
        }
    }

    private static TimeSpan DelayUntilNextRun()
    {
        var now = VietnamTime.Now;
        var nextRun = now.DateTime.Date.Add(ScheduleTime.ToTimeSpan());

        if (nextRun <= now.DateTime)
            nextRun = nextRun.AddDays(1);

        return nextRun - now.DateTime;
    }
}
