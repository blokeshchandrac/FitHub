using FitHub.Application.Bookings;
using FitHub.Domain.Auditing;
using FitHub.Domain.Bookings;
using FitHub.Domain.Identity;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Bookings;

public sealed class ClassBookingService(
    FitHubDbContext db,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : IClassBookingService
{
    public async Task<IReadOnlyCollection<ClassScheduleDto>> ListForMemberAsync(Guid userId, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var items = await Query().Where(item => item.Status == ClassScheduleStatus.Approved && item.StartsAt > now)
            .OrderBy(item => item.StartsAt).ToListAsync(ct);
        return items.Select(item => Map(item, userId)).ToList();
    }

    public Task<IReadOnlyCollection<ClassScheduleDto>> ListAsync(Guid userId, CancellationToken ct = default) =>
        ListForMemberAsync(userId, ct);

    public async Task<IReadOnlyCollection<ClassScheduleDto>> ListForTrainerAsync(Guid trainerId, CancellationToken ct = default)
    {
        var items = await Query().Where(item => item.InstructorUserId == trainerId)
            .OrderByDescending(item => item.StartsAt).ToListAsync(ct);
        return items.Select(item => Map(item, Guid.Empty)).ToList();
    }

    public async Task<IReadOnlyCollection<ClassScheduleDto>> ListForAdministrationAsync(CancellationToken ct = default)
    {
        var items = await Query().OrderBy(item => item.Status).ThenBy(item => item.StartsAt).ToListAsync(ct);
        return items.Select(item => Map(item, Guid.Empty)).ToList();
    }

    public async Task<ClassScheduleDto> CreateAsync(CreateClassScheduleCommand command, CancellationToken ct = default)
    {
        await EnsureTrainerAsync(command.InstructorUserId);
        ValidateFuture(command);
        var item = new ClassSchedule(command.Name, command.Description, command.StartsAt, command.EndsAt,
            command.Capacity, command.InstructorUserId, command.Location);
        db.ClassSchedules.Add(item);
        AddAudit(command.InstructorUserId, "ClassSubmitted", item.Id, "Trainer submitted class for administrator approval.");
        await db.SaveChangesAsync(ct);
        await db.Entry(item).Reference(x => x.InstructorUser).LoadAsync(ct);
        return Map(item, Guid.Empty);
    }

    public async Task<ClassScheduleDto?> UpdateAsync(Guid scheduleId, Guid trainerId, CreateClassScheduleCommand command, CancellationToken ct = default)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.Id == scheduleId && x.InstructorUserId == trainerId, ct);
        if (item is null) return null;
        ValidateFuture(command);
        item.Update(command.Name, command.Description, command.StartsAt, command.EndsAt, command.Capacity,
            command.Location, timeProvider.GetUtcNow());
        AddAudit(trainerId, "ClassResubmitted", item.Id, "Trainer edited and resubmitted class for approval.");
        await db.SaveChangesAsync(ct);
        return Map(item, Guid.Empty);
    }

    public async Task<ClassScheduleDto?> ReviewAsync(Guid scheduleId, Guid administratorId, ReviewClassScheduleCommand command, CancellationToken ct = default)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.Id == scheduleId, ct);
        if (item is null) return null;
        if (command.Approve) item.Approve(administratorId, timeProvider.GetUtcNow());
        else item.Reject(administratorId, command.Reason ?? string.Empty, timeProvider.GetUtcNow());
        AddAudit(administratorId, command.Approve ? "ClassApproved" : "ClassRejected", item.Id,
            command.Approve ? "Administrator approved trainer class." : "Administrator rejected trainer class.");
        await db.SaveChangesAsync(ct);
        return Map(item, Guid.Empty);
    }

    public async Task<IReadOnlyCollection<ClassParticipantDto>?> ListParticipantsAsync(
        Guid scheduleId, Guid staffUserId, bool administrator, CancellationToken ct = default)
    {
        var schedule = await db.ClassSchedules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == scheduleId, ct);
        if (schedule is null) return null;
        if (!administrator && schedule.InstructorUserId != staffUserId) throw new UnauthorizedAccessException();
        return await db.ClassBookings.AsNoTracking().Where(x => x.ClassScheduleId == scheduleId && x.Status != BookingStatus.Cancelled)
            .Include(x => x.User).OrderBy(x => x.BookedAt)
            .Select(x => new ClassParticipantDto(x.UserId,
                ((x.User.PreferredName ?? (x.User.FirstName + " " + x.User.LastName))).Trim(),
                x.User.Email ?? string.Empty, x.Status.ToString(), x.BookedAt))
            .ToListAsync(ct);
    }

    public async Task<ClassScheduleDto?> RecordAttendanceAsync(
        Guid scheduleId, Guid memberId, Guid staffUserId, bool administrator, bool attended, CancellationToken ct = default)
    {
        var schedule = await Query().SingleOrDefaultAsync(x => x.Id == scheduleId, ct);
        if (schedule is null) return null;
        if (!administrator && schedule.InstructorUserId != staffUserId) throw new UnauthorizedAccessException();
        if (schedule.StartsAt > timeProvider.GetUtcNow())
            throw new InvalidOperationException("Class attendance can be recorded only after the session starts.");
        var booking = schedule.Bookings.SingleOrDefault(x => x.UserId == memberId)
            ?? throw new InvalidOperationException("The member is not registered for this class.");
        booking.RecordAttendance(attended, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return Map(schedule, Guid.Empty);
    }

    public Task<ClassScheduleDto?> BookAsync(Guid id, Guid userId, CancellationToken ct = default) => ChangeLocked(id, userId, true, ct);
    public Task<ClassScheduleDto?> CancelAsync(Guid id, Guid userId, CancellationToken ct = default) => ChangeLocked(id, userId, false, ct);

    private async Task<ClassScheduleDto?> ChangeLocked(Guid id, Guid userId, bool book, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Neon retry must encompass the row lock, state transition and commit as one unit.
            // Clearing tracked state makes a retry reload the authoritative persisted booking.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var schedule = await db.ClassSchedules.FromSql($"SELECT * FROM \"ClassSchedules\" WHERE \"Id\" = {id} FOR UPDATE")
                .Include(x => x.InstructorUser).SingleOrDefaultAsync(ct);
            if (schedule is null) return null;
            await db.Entry(schedule).Collection(x => x.Bookings).LoadAsync(ct);
            var now = timeProvider.GetUtcNow();
            if (schedule.Status != ClassScheduleStatus.Approved) throw new InvalidOperationException("Only an approved class can be booked.");
            if (schedule.StartsAt <= now) throw new InvalidOperationException("This class has already started.");
            var mine = schedule.Bookings.SingleOrDefault(x => x.UserId == userId);
            if (book)
            {
                var confirmed = schedule.Bookings.Count(x => x.Status == BookingStatus.Confirmed);
                var status = confirmed < schedule.Capacity ? BookingStatus.Confirmed : BookingStatus.Waitlisted;
                if (mine is null) { mine = new ClassBooking(schedule.Id, userId, status, now); schedule.Bookings.Add(mine); }
                else mine.Rebook(status, now);
            }
            else
            {
                if (mine is null || mine.Status == BookingStatus.Cancelled) throw new InvalidOperationException("No active booking exists.");
                var released = mine.Status == BookingStatus.Confirmed;
                mine.Cancel(now);
                if (released)
                {
                    var next = schedule.Bookings.Where(x => x.Status == BookingStatus.Waitlisted).OrderBy(x => x.BookedAt).FirstOrDefault();
                    next?.Promote(now);
                }
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Map(schedule, userId);
        });
    }

    private IQueryable<ClassSchedule> Query() => db.ClassSchedules.Include(x => x.InstructorUser).Include(x => x.Bookings);

    private async Task EnsureTrainerAsync(Guid userId)
    {
        var instructor = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ArgumentException("Trainer not found.");
        if (!await userManager.IsInRoleAsync(instructor, "Trainer")) throw new ArgumentException("Instructor must have the Trainer role.");
    }

    private void ValidateFuture(CreateClassScheduleCommand command)
    {
        if (command.StartsAt <= timeProvider.GetUtcNow()) throw new ArgumentException("Class must start in the future.");
    }

    private static ClassScheduleDto Map(ClassSchedule item, Guid userId) => new(
        item.Id, item.Name, item.Description, item.StartsAt, item.EndsAt, item.Capacity,
        item.Bookings.Count(x => x.Status is BookingStatus.Confirmed or BookingStatus.Attended or BookingStatus.NoShow),
        item.Bookings.Count(x => x.Status == BookingStatus.Waitlisted),
        item.InstructorUserId,
        item.InstructorUser is null ? null : ((item.InstructorUser.PreferredName ?? (item.InstructorUser.FirstName + " " + item.InstructorUser.LastName))).Trim(),
        item.Location, item.Status.ToString(), item.ReviewReason,
        item.Bookings.SingleOrDefault(x => x.UserId == userId)?.Status.ToString());

    private void AddAudit(Guid userId, string action, Guid scheduleId, string details) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = "ClassSchedule",
            EntityId = scheduleId,
            Details = details,
            OccurredAt = timeProvider.GetUtcNow()
        });
}
