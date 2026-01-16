using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieBooking.Api.Data;

namespace MovieBooking.Api.Workers;

public class SeatCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeatCleanupWorker> _logger;

    public SeatCleanupWorker(IServiceProvider serviceProvider, ILogger<SeatCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Seat Cleanup Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

                // Efficient Batch Update (NET 8+ Feature)
                var rowsAffected = await context.Seats
                    .Where(s => s.HoldExpiry != null && s.HoldExpiry < DateTimeOffset.UtcNow && s.BookingId == null)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(s => s.HoldExpiry, (DateTimeOffset?)null)
                        .SetProperty(s => s.UserId, (string?)null), 
                        stoppingToken);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Released {Count} expired seat holds.", rowsAffected);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during seat cleanup.");
            }

            // Run every 5 seconds for responsiveness in this demo
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
