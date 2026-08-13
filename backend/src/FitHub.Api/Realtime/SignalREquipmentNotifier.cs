using FitHub.Application.Equipment;
using Microsoft.AspNetCore.SignalR;
namespace FitHub.Api.Realtime;
public sealed class SignalREquipmentNotifier(IHubContext<EquipmentHub> hubContext) : IEquipmentRealtimeNotifier
{
    public Task EquipmentChangedAsync(EquipmentChangedDto change, CancellationToken cancellationToken = default) => hubContext.Clients.All.SendAsync("equipmentChanged", change, cancellationToken);
}
