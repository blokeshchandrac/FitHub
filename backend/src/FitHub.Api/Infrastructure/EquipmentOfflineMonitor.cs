using FitHub.Application.Equipment;
namespace FitHub.Api.Infrastructure;
public sealed class EquipmentOfflineMonitor(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EquipmentOfflineMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(configuration.GetValue("EquipmentDevices:MonitorIntervalSeconds", 30));
        var timeout = TimeSpan.FromSeconds(configuration.GetValue("EquipmentDevices:HeartbeatTimeoutSeconds", 90));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await using var scope = scopeFactory.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<IEquipmentService>().MarkStaleDevicesOfflineAsync(timeout, stoppingToken); }
            catch (Exception exception) when (exception is not OperationCanceledException) { logger.LogError(exception, "Equipment offline detection failed."); }
        }
    }
}
