using Booking.Service.Interfaces;

namespace Booking.API.Workers;

// AI contribution: Copilot — periodic retention-expiration worker.
public sealed class BookingRetentionExpirationWorker : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingRetentionExpirationWorker> _logger;

    public BookingRetentionExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingRetentionExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var expiredCount = await bookingService.ExpireRetentionsAsync(stoppingToken);

                if (expiredCount > 0)
                {
                    _logger.LogInformation(
                        "Expired {Count} booking retention(s).",
                        expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire booking retentions.");
            }

            try
            {
                await Task.Delay(SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
