using FitHub.Application.Equipment;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.EquipmentServices;
public sealed class EquipmentAccessService(FitHubDbContext dbContext, TimeProvider timeProvider) : IEquipmentAccessService
{
    public Task<bool> IsCheckedInAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow().AddHours(-18);
        return dbContext.Attendances.AnyAsync(x => x.UserId == userId && x.CheckedOutAt == null && x.CheckedInAt >= cutoff, cancellationToken);
    }
}
