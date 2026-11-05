using Booking.Infrastructure.Entities;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                var now = DateTime.UtcNow;

                // Update expired holds in one statement instead of loading each row.
                var expiredCount = await context.Bookings
                    .Where(booking =>
                        booking.Status == BookingStatus.Retenida &&
                        booking.ExpiresAt <= now)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            booking => booking.Status,
                            BookingStatus.Vencida),
                        stoppingToken);

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
