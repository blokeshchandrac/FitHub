using FitHub.Application.Equipment;
using FitHub.Application.FaultReports;
using FitHub.Domain.Equipment;
using FitHub.Infrastructure.Persistence;
using FitHub.Application.Notifications;
using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.FaultReports;

public sealed class FaultReportService(FitHubDbContext dbContext, IPhotoStorage photoStorage, IEquipmentRealtimeNotifier notifier, INotificationService notificationService, TimeProvider timeProvider) : IFaultReportService
{
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase) { ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp" };
    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    public async Task<FaultReportDto> CreateAsync(Guid userId, CreateFaultReportCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<FaultCategory>(command.Category, true, out var category)) throw new ArgumentException("Unknown fault category.");
        var equipment = await dbContext.Equipment.Include(x => x.StatusHistory).SingleOrDefaultAsync(x => x.Id == command.EquipmentId, cancellationToken) ?? throw new KeyNotFoundException("Equipment not found.");
        var report = new FaultReport(equipment.Id, userId, category, command.Comment, command.SeriousSafetyConcern);
        dbContext.FaultReports.Add(report);
        if (command.Photo is not null)
        {
            if (command.Photo.Length <= 0 || command.Photo.Length > MaxPhotoBytes || !Types.TryGetValue(command.Photo.ContentType, out var extension))
                throw new ArgumentException("Photographs must be JPG, PNG or WebP and no larger than 5 MB.");
            await using var verifiedContent = new MemoryStream((int)command.Photo.Length);
            await command.Photo.Content.CopyToAsync(verifiedContent, cancellationToken);
            if (verifiedContent.Length != command.Photo.Length || !HasValidSignature(verifiedContent.ToArray(), extension))
                throw new ArgumentException("The photograph content does not match its declared file type.");
            verifiedContent.Position = 0;
            var url = await photoStorage.SaveAsync(verifiedContent, extension, cancellationToken);
            report.Images.Add(new FaultReportImage(report.Id, url));
        }
        if (report.IsSeriousSafetyConcern) equipment.EnterMaintenance(timeProvider.GetUtcNow(), userId, "Serious safety fault report");
        await dbContext.SaveChangesAsync(cancellationToken);
        if (report.IsSeriousSafetyConcern) await notifier.EquipmentChangedAsync(new(equipment.Id, equipment.CurrentState.ToString(), equipment.Version, timeProvider.GetUtcNow()), cancellationToken);
        return Map(report, equipment.Name);
    }

    public async Task<IReadOnlyCollection<FaultReportDto>> ListForMemberAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await Query().Where(x => x.ReportedByUserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(MapProjection).ToList();

    public async Task<IReadOnlyCollection<FaultReportDto>> ListForAdministrationAsync(CancellationToken cancellationToken = default) =>
        (await Query().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(MapProjection).ToList();

    public async Task<FaultReportDto?> UpdateAsync(Guid reportId, Guid administratorId, UpdateFaultReportCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<FaultReportStatus>(command.Status, true, out var status)) throw new ArgumentException("Unknown fault status.");
        var report = await dbContext.FaultReports.Include(x => x.Equipment).Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken);
        if (report is null) return null;
        report.UpdateStatus(status, command.MaintenanceNotes, timeProvider.GetUtcNow());
        if (status is FaultReportStatus.UnderReview or FaultReportStatus.Scheduled or FaultReportStatus.Resolved)
            dbContext.MaintenanceActions.Add(new MaintenanceAction { EquipmentId = report.EquipmentId, FaultReportId = report.Id, PerformedByUserId = administratorId, ActionType = status == FaultReportStatus.Resolved ? MaintenanceActionType.Repair : MaintenanceActionType.Inspection, Description = command.MaintenanceNotes, PerformedAt = timeProvider.GetUtcNow() });
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.CreateFaultStatusAsync(report.ReportedByUserId,report.Id,report.Equipment.Name,status.ToString(),cancellationToken);
        return Map(report, report.Equipment.Name);
    }

    private IQueryable<FaultReport> Query() => dbContext.FaultReports.AsNoTracking().Include(x => x.Equipment).Include(x => x.Images);
    private static bool HasValidSignature(byte[] bytes, string extension) => extension switch
    {
        ".jpg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
        ".webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
    private static FaultReportDto MapProjection(FaultReport x) => new(x.Id, x.EquipmentId, x.Equipment.Name, x.ReportedByUserId, x.Category.ToString(), x.Comment, x.IsSeriousSafetyConcern, x.Status.ToString(), x.MaintenanceNotes, x.ResolutionDate, x.Images.Select(i => i.ImageUrl).ToList(), x.CreatedAt);
    private static FaultReportDto Map(FaultReport x, string equipmentName) => new(x.Id, x.EquipmentId, equipmentName, x.ReportedByUserId, x.Category.ToString(), x.Comment, x.IsSeriousSafetyConcern, x.Status.ToString(), x.MaintenanceNotes, x.ResolutionDate, x.Images.Select(i => i.ImageUrl).ToList(), x.CreatedAt);
}
