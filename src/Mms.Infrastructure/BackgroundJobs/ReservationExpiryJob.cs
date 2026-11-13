using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mms.Application.Abstractions.Messaging;
using Mms.Application.Features.Reservations.Commands;

namespace Mms.Infrastructure.BackgroundJobs;

/// <summary>
/// Mỗi phút kiểm tra một lần: giữ phòng quá hạn sẽ chuyển Expired và phòng được trả về Available.
/// </summary>
public sealed class ReservationExpiryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (true)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Không hết hạn được giữ phòng, sẽ thử lại ở chu kỳ sau.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ExpireReservationsCommand, int>>();

        var expired = await handler.HandleAsync(new ExpireReservationsCommand(), ct);

        if (expired > 0)
            logger.LogInformation("Đã chuyển {Count} giữ phòng sang Expired.", expired);
    }
}
