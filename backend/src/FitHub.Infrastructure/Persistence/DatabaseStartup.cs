using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FitHub.Infrastructure.Persistence;

public static class DatabaseStartup
{
    /// <summary>
    /// Applies pending migrations with bounded retries so a sleeping hosted PostgreSQL compute can wake
    /// without allowing startup to drop, recreate, or bypass the migration history.
    /// </summary>
    public static async Task ApplyMigrationsAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("FitHub.DatabaseStartup");
        Exception? lastFailure = null;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<FitHubDbContext>()
                    .Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations are current.");
                return;
            }
            catch (Exception exception) when (attempt < 5 && !cancellationToken.IsCancellationRequested)
            {
                lastFailure = exception;
                var delay = TimeSpan.FromSeconds(attempt * 2);
                logger.LogWarning("Database migration attempt {Attempt} failed; retrying in {DelaySeconds} seconds.",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Database migrations could not be applied after the configured startup retries.",
            lastFailure);
    }
}
