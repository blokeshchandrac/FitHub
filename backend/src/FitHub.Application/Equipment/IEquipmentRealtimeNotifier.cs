namespace FitHub.Application.Equipment;
public interface IEquipmentRealtimeNotifier { Task EquipmentChangedAsync(EquipmentChangedDto change, CancellationToken cancellationToken = default); }
