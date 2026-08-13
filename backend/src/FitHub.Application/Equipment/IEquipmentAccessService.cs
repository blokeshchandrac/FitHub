namespace FitHub.Application.Equipment;
public interface IEquipmentAccessService { Task<bool> IsCheckedInAsync(Guid userId, CancellationToken cancellationToken = default); }
