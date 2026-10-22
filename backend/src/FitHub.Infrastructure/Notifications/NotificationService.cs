using FitHub.Application.Analytics;
using FitHub.Application.Common;
using FitHub.Application.Notifications;
using FitHub.Domain.Bookings;
using FitHub.Domain.Communications;
using FitHub.Domain.Memberships;
using FitHub.Domain.Workouts;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Notifications;

public sealed class NotificationService(FitHubDbContext db, TimeProvider timeProvider) : INotificationService
{
    private static readonly IReadOnlyDictionary<NotificationType, (string Name, string Description)> CategoryDetails =
        new Dictionary<NotificationType, (string, string)>
        {
            [NotificationType.MembershipExpiry] = ("Membership renewal", "Reminders when an active membership is approaching its end date."),
            [NotificationType.ClassReminder] = ("Class reminders", "Reminders for approved fitness classes you have booked."),
            [NotificationType.WorkoutConsistency] = ("Training reminders", "Prompts when no completed workout has been recorded recently."),
            [NotificationType.StreakWarning] = ("Streak risk", "Warnings when an established attendance streak may be interrupted."),
            [NotificationType.PersonalRecord] = ("Personal records", "Updates when a completed workout establishes a recorded best."),
            [NotificationType.FaultReportStatusUpdate] = ("Equipment report updates", "Updates when the status of your equipment report changes.")
        };

    public async Task<IReadOnlyCollection<NotificationDto>> GetAsync(Guid userId, bool generateMemberNotifications = true, CancellationToken ct = default)
    {
        if (generateMemberNotifications) await GenerateAsync(userId, ct);
        return await db.Notifications.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt).Take(100)
            .Select(item => new NotificationDto(item.Id, item.Type.ToString(), item.Title, item.Message, item.IsRead, item.CreatedAt, item.ReadAt))
            .ToListAsync(ct);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await db.Notifications.SingleOrDefaultAsync(notification => notification.Id == id && notification.UserId == userId, ct);
        if (item is null) return false;
        item.MarkRead(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task CreateFaultStatusAsync(Guid userId, Guid reportId, string equipmentName, string status, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(userId, NotificationType.FaultReportStatusUpdate, ct)) return;
        var key = $"fault:{reportId}:{status}";
        if (await db.Notifications.AnyAsync(item => item.UserId == userId && item.DeduplicationKey == key, ct)) return;
        db.Notifications.Add(new Notification(userId, NotificationType.FaultReportStatusUpdate, "Fault report updated", $"Your report for {equipmentName} is now {status}.", key));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsurePreferencesAsync(userId, ct);
        return await ReadPreferencesAsync(userId, ct);
    }

    public async Task<IReadOnlyCollection<NotificationPreferenceDto>> UpdatePreferencesAsync(Guid userId, IReadOnlyCollection<NotificationPreferenceUpdate> updates, CancellationToken ct = default)
    {
        await EnsurePreferencesAsync(userId, ct);
        var preferences = await db.NotificationPreferences.Where(item => item.UserId == userId).ToDictionaryAsync(item => item.Category, ct);
        var now = timeProvider.GetUtcNow();
        foreach (var update in updates)
        {
            if (!Enum.TryParse<NotificationType>(update.Category, true, out var category) || !CategoryDetails.ContainsKey(category))
                throw new CommandValidationException(new Dictionary<string, string[]>
                {
                    ["category"] = [$"Notification category '{update.Category}' is not supported."]
                });
            preferences[category].SetEnabled(update.Enabled, now);
        }
        await db.SaveChangesAsync(ct);
        return await ReadPreferencesAsync(userId, ct);
    }

    private async Task GenerateAsync(Guid userId, CancellationToken ct)
    {
        var enabled = await GetEnabledCategoriesAsync(userId, ct);
        var now = timeProvider.GetUtcNow();
        var existing = (await db.Notifications.AsNoTracking().Where(item => item.UserId == userId).Select(item => item.DeduplicationKey).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var pending = new List<Notification>();

        var membership = await db.Memberships.AsNoTracking().Where(item => item.UserId == userId && item.Status == MembershipStatus.Active && item.EndsAt > now && item.EndsAt <= now.AddDays(7)).OrderBy(item => item.EndsAt).FirstOrDefaultAsync(ct);
        if (membership is not null) Add(NotificationType.MembershipExpiry, "Membership expiring", $"Your membership expires on {membership.EndsAt:yyyy-MM-dd}.", $"membership-expiry:{membership.Id}");

        var classes = await db.ClassBookings.AsNoTracking().Include(item => item.ClassSchedule).Where(item => item.UserId == userId && item.Status == BookingStatus.Confirmed && item.ClassSchedule.Status == ClassScheduleStatus.Approved && item.ClassSchedule.StartsAt > now && item.ClassSchedule.StartsAt <= now.AddHours(24)).ToListAsync(ct);
        foreach (var booking in classes) Add(NotificationType.ClassReminder, "Class reminder", $"{booking.ClassSchedule.Name} starts at {booking.ClassSchedule.StartsAt:g}.", $"class-reminder:{booking.Id}");

        var sessions = await db.WorkoutSessions.AsNoTracking().Include(item => item.ExerciseLogs).ThenInclude(item => item.Exercise).Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets).Where(item => item.UserId == userId && item.Status == WorkoutSessionStatus.Completed).OrderByDescending(item => item.EndedAt).ToListAsync(ct);
        var last = sessions.FirstOrDefault()?.EndedAt;
        if (last is null || now - last.Value >= TimeSpan.FromDays(7)) Add(NotificationType.WorkoutConsistency, "Workout consistency", "No completed workout has been recorded in the last 7 days. Plan a manageable session when recovered.", $"workout-consistency:{now:yyyy-MM-dd}");

        var attendance = await db.Attendances.AsNoTracking().Where(item => item.UserId == userId && item.CheckedInAt >= now.AddDays(-90)).Select(item => item.CheckedInAt).ToListAsync(ct);
        var week = AnalyticsRules.StartOfUtcWeek(now);
        var previousStreak = AnalyticsRules.CurrentWeeklyStreak(attendance, now.AddDays(-7));
        if (previousStreak >= 2 && !attendance.Any(item => item >= week)) Add(NotificationType.StreakWarning, "Attendance streak warning", $"Your previous attendance streak was {previousStreak} weeks and no visit is recorded this week.", $"streak-warning:{week:yyyy-MM-dd}");

        var sets = sessions.SelectMany(session => session.ExerciseLogs.SelectMany(log => log.Sets.Where(set => set.WeightKg > 0).Select(set => new { Set = set, log.Exercise.Name, log.ExerciseId, At = session.EndedAt ?? session.StartedAt }))).ToList();
        foreach (var group in sets.GroupBy(item => new { item.ExerciseId, item.Name }))
        {
            var max = group.Max(item => item.Set.WeightKg!.Value);
            var record = group.Where(item => item.Set.WeightKg == max).OrderBy(item => item.At).ThenBy(item => item.Set.SetNumber).First();
            if (record.At >= now.AddDays(-7)) Add(NotificationType.PersonalRecord, "Personal record", $"New recorded best for {record.Name}: {max:0.#} kg.", $"personal-record:{record.Set.Id}");
        }

        if (pending.Count > 0)
        {
            db.Notifications.AddRange(pending);
            await db.SaveChangesAsync(ct);
        }

        void Add(NotificationType type, string title, string message, string key)
        {
            if (enabled.Contains(type) && existing.Add(key)) pending.Add(new Notification(userId, type, title, message, key));
        }
    }

    private async Task<bool> IsEnabledAsync(Guid userId, NotificationType category, CancellationToken ct)
    {
        await EnsurePreferencesAsync(userId, ct);
        return await db.NotificationPreferences.AsNoTracking().AnyAsync(item => item.UserId == userId && item.Category == category && item.Enabled, ct);
    }

    private async Task<HashSet<NotificationType>> GetEnabledCategoriesAsync(Guid userId, CancellationToken ct)
    {
        await EnsurePreferencesAsync(userId, ct);
        return (await db.NotificationPreferences.AsNoTracking().Where(item => item.UserId == userId && item.Enabled).Select(item => item.Category).ToListAsync(ct)).ToHashSet();
    }

    private async Task EnsurePreferencesAsync(Guid userId, CancellationToken ct)
    {
        var existing = await db.NotificationPreferences.Where(item => item.UserId == userId).Select(item => item.Category).ToListAsync(ct);
        var missing = Enum.GetValues<NotificationType>().Except(existing).ToArray();
        if (missing.Length == 0) return;
        db.NotificationPreferences.AddRange(missing.Select(category => new NotificationPreference(userId, category)));
        await db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyCollection<NotificationPreferenceDto>> ReadPreferencesAsync(Guid userId, CancellationToken ct) =>
        (await db.NotificationPreferences.AsNoTracking().Where(item => item.UserId == userId).OrderBy(item => item.Category).ToListAsync(ct))
        .Select(item =>
        {
            var details = CategoryDetails[item.Category];
            return new NotificationPreferenceDto(item.Category.ToString(), details.Name, details.Description, item.Enabled, true);
        }).ToArray();
}
