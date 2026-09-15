using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamplePlugin.Hosting;

/// <summary>
/// Reference background service returned through <c>GetHostedServices</c>.
/// </summary>
/// <remarks>
/// The host registers every service returned by a plugin's <c>GetHostedServices</c>
/// as a singleton <see cref="IHostedService"/> and starts and stops it with the
/// application. The service simply counts its polling cycles to demonstrate that
/// it was started and stopped by the host.
/// </remarks>
public sealed class ExampleBackgroundService(
    ILogger<ExampleBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private long _cycles;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Example background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _cycles);
            logger.LogDebug("Example background service cycle {Cycle}.", _cycles);
            await Task.Delay(TimeSpan.FromSeconds(30), timeProvider, stoppingToken);
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Example background service stopping after {Cycles} cycles.", _cycles);
        await base.StopAsync(cancellationToken);
    }
}