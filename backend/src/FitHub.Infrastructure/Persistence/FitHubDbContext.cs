using FitHub.Domain.Attendance;
using FitHub.Domain.Auditing;
using FitHub.Domain.Bookings;
using FitHub.Domain.Common;
using FitHub.Domain.Communications;
using FitHub.Domain.Equipment;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Domain.Parking;
using FitHub.Domain.Payments;
using FitHub.Domain.Progress;
using FitHub.Domain.Workouts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Persistence;

public sealed class FitHubDbContext(DbContextOptions<FitHubDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProcessedPaymentWebhook> ProcessedPaymentWebhooks => Set<ProcessedPaymentWebhook>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AttendanceCheckInToken> AttendanceCheckInTokens => Set<AttendanceCheckInToken>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutPlanAssignment> WorkoutPlanAssignments => Set<WorkoutPlanAssignment>();
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();
    public DbSet<SetLog> SetLogs => Set<SetLog>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentStatusEvent> EquipmentStatusEvents => Set<EquipmentStatusEvent>();
    public DbSet<EquipmentDevice> EquipmentDevices => Set<EquipmentDevice>();
    public DbSet<MemberAccessCard> MemberAccessCards => Set<MemberAccessCard>();
    public DbSet<FaultReport> FaultReports => Set<FaultReport>();
    public DbSet<FaultReportImage> FaultReportImages => Set<FaultReportImage>();
    public DbSet<MaintenanceAction> MaintenanceActions => Set<MaintenanceAction>();
    public DbSet<ParkingSpace> ParkingSpaces => Set<ParkingSpace>();
    public DbSet<ParkingStatusEvent> ParkingStatusEvents => Set<ParkingStatusEvent>();
    public DbSet<ParkingCamera> ParkingCameras => Set<ParkingCamera>();
    public DbSet<ParkingViolationAlert> ParkingViolationAlerts => Set<ParkingViolationAlert>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<ClassBooking> ClassBookings => Set<ClassBooking>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<GymBranch> GymBranches => Set<GymBranch>();
    public DbSet<TrainerMemberAssignment> TrainerMemberAssignments => Set<TrainerMemberAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(FitHubDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Entity.MarkUpdated(now);
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Entity.MarkUpdated(now);
        }
    }
}
