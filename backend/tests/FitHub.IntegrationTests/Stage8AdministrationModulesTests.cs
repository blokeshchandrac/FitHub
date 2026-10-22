using FitHub.Application.Administration;using FitHub.Application.Analytics;using FitHub.Application.Bookings;using FitHub.Application.Common;using FitHub.Application.Notifications;using FitHub.Domain.Attendance;using FitHub.Domain.Bookings;using FitHub.Domain.Communications;using FitHub.Domain.Identity;using FitHub.Domain.Memberships;using FitHub.Domain.Payments;using FitHub.Domain.Progress;using FitHub.Domain.Workouts;using FitHub.Infrastructure.Persistence;using Microsoft.AspNetCore.Hosting;using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc.Testing;using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.DependencyInjection;using Testcontainers.PostgreSql;
namespace FitHub.IntegrationTests;
public sealed class Stage8AdministrationModulesTests:IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres=new PostgreSqlBuilder("postgres:18-alpine").WithDatabase("fithub_stage8_tests").WithUsername("stage8").WithPassword("Test-only-password-42!").Build();private WebApplicationFactory<Program> factory=null!;
    public async Task InitializeAsync(){await postgres.StartAsync();factory=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{builder.UseEnvironment("Testing");builder.UseSetting("ConnectionStrings:FitHubDatabase",postgres.GetConnectionString());builder.UseSetting("Jwt:SigningKey","FitHub-tests-only-signing-key-at-least-32-characters-long");builder.UseSetting("SeedData:Enabled","false");});await using var scope=factory.Services.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Database.MigrateAsync();}
    public async Task DisposeAsync(){await factory.DisposeAsync();await postgres.DisposeAsync();}

    [Fact]public async Task CapacityCreatesWaitlistDuplicateIsRejectedAndCancellationPromotes()
    {
        var trainer=await AddUser("trainer8@test.local","Trainer");var administrator=await AddUser("admin8@test.local","Administrator");var member1=await AddUser("member81@test.local","Member");var member2=await AddUser("member82@test.local","Member");await using var scope=factory.Services.CreateAsyncScope();var service=scope.ServiceProvider.GetRequiredService<IClassBookingService>();var schedule=await service.CreateAsync(new("Capacity class",null,DateTimeOffset.UtcNow.AddDays(1),DateTimeOffset.UtcNow.AddDays(1).AddHours(1),1,trainer));await service.ReviewAsync(schedule.Id,administrator,new(true,null));
        Assert.Equal("Confirmed",(await service.BookAsync(schedule.Id,member1))!.MyStatus);Assert.Equal("Waitlisted",(await service.BookAsync(schedule.Id,member2))!.MyStatus);await Assert.ThrowsAsync<InvalidOperationException>(()=>service.BookAsync(schedule.Id,member1));await service.CancelAsync(schedule.Id,member1);Assert.Equal("Confirmed",(await service.ListAsync(member2)).Single().MyStatus);var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();Assert.Equal(1,await db.ClassBookings.CountAsync(x=>x.ClassScheduleId==schedule.Id&&x.Status==BookingStatus.Confirmed));Assert.Equal(0,await db.ClassBookings.CountAsync(x=>x.ClassScheduleId==schedule.Id&&x.Status==BookingStatus.Waitlisted));var activity=await scope.ServiceProvider.GetRequiredService<IAdministrationManagementService>().ListActivityAsync();Assert.Contains(activity,x=>x.Action=="ClassSubmitted"&&x.EntityId==schedule.Id);Assert.Contains(activity,x=>x.Action=="ClassApproved"&&x.EntityId==schedule.Id);
    }

    [Fact]
    public async Task DashboardRevenueAndCsvUseConfirmedNetPaymentsAndExcludeDemoSeed()
    {
        var member = await AddUser("revenue8@test.local", "Member");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        var plan = new MembershipPlan { Name = "Monthly", Price = 29.90m, BillingPeriodMonths = 1 };
        db.MembershipPlans.Add(plan);

        var succeeded = new Payment { UserId = member, MembershipPlanId = plan.Id, PurchaseKind = MembershipPurchaseKind.Purchase, Amount = 29.90m };
        succeeded.MarkSucceeded(DateTimeOffset.UtcNow, "pi_succeeded_stage8");
        var partiallyRefunded = new Payment { UserId = member, MembershipPlanId = plan.Id, PurchaseKind = MembershipPurchaseKind.Renewal, Amount = 30m };
        partiallyRefunded.MarkSucceeded(DateTimeOffset.UtcNow, "pi_refunded_stage8");
        partiallyRefunded.RecordRefund(10m, DateTimeOffset.UtcNow);
        var seededDemo = new Payment { UserId = member, MembershipPlanId = plan.Id, PurchaseKind = MembershipPurchaseKind.Renewal, Amount = 100m, IsSeededDemo = true };
        seededDemo.MarkSucceeded(DateTimeOffset.UtcNow, "pi_demo_stage8");
        var pending = new Payment { UserId = member, MembershipPlanId = plan.Id, PurchaseKind = MembershipPurchaseKind.Renewal, Amount = 75m };
        db.Payments.AddRange(succeeded, partiallyRefunded, seededDemo, pending);
        await db.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IAdministratorDashboardService>();
        var dashboard = await service.GetAsync();
        var csv = await service.ExportAsync("payments");

        Assert.Equal(49.90m, dashboard.CompletedRevenue);
        Assert.Contains(succeeded.Id.ToString(), csv.Content);
        Assert.Contains(partiallyRefunded.Id.ToString(), csv.Content);
        Assert.DoesNotContain(seededDemo.Id.ToString(), csv.Content);
        Assert.DoesNotContain(pending.Id.ToString(), csv.Content);
    }

    [Fact]public async Task NotificationGenerationCoversAllSixRequiredTypesAndDeduplicates()
    {
        var user=await AddUser("notify8@test.local","Member");var instructor=await AddUser("notifytrainer8@test.local","Trainer");await using var scope=factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();var now=DateTimeOffset.UtcNow;var plan=new MembershipPlan{Name="Notify plan",Price=10m,BillingPeriodMonths=1};db.MembershipPlans.Add(plan);var membership=new Membership(user,plan.Id,now.AddDays(-20),now.AddDays(3));membership.Activate(now.AddDays(-20),now.AddDays(3));db.Memberships.Add(membership);var schedule=new ClassSchedule("Reminder class",null,now.AddHours(2),now.AddHours(3),5,instructor);schedule.Approve(instructor,now);db.ClassSchedules.Add(schedule);db.ClassBookings.Add(new ClassBooking(schedule.Id,user,BookingStatus.Confirmed,now));var week=AnalyticsRules.StartOfUtcWeek(now);db.Attendances.AddRange(new Attendance(user,week.AddDays(-7)),new Attendance(user,week.AddDays(-14)));await db.SaveChangesAsync();var service=scope.ServiceProvider.GetRequiredService<INotificationService>();var initial=await service.GetAsync(user);Assert.Contains(initial,x=>x.Type=="MembershipExpiry");Assert.Contains(initial,x=>x.Type=="ClassReminder");Assert.Contains(initial,x=>x.Type=="WorkoutConsistency");Assert.Contains(initial,x=>x.Type=="StreakWarning");
        var exercise=new Exercise{Name="Stage 8 press",MuscleGroup="Chest"};var session=new WorkoutSession{UserId=user,StartedAt=now.AddHours(-1)};session.Start(now.AddHours(-1));session.Complete(now);var log=new ExerciseLog{WorkoutSessionId=session.Id,ExerciseId=exercise.Id,Exercise=exercise};log.Sets.Add(new SetLog{ExerciseLogId=log.Id,SetNumber=1,Repetitions=8,WeightKg=80m});session.ExerciseLogs.Add(log);db.Exercises.Add(exercise);db.WorkoutSessions.Add(session);await db.SaveChangesAsync();await service.CreateFaultStatusAsync(user,Guid.NewGuid(),"Treadmill","Resolved");var all=await service.GetAsync(user);Assert.Contains(all,x=>x.Type=="PersonalRecord");Assert.Contains(all,x=>x.Type=="FaultReportStatusUpdate");var count=all.Count;Assert.Equal(count,(await service.GetAsync(user)).Count);
    }

    [Fact]
    public async Task NotificationPreferencesPersistEnforceOptionalDeliveryAndPreserveExistingNotifications()
    {
        var user = await AddUser("preferences8@test.local", "Member");
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var defaults = await service.GetPreferencesAsync(user);
        Assert.Equal(Enum.GetValues<NotificationType>().Length, defaults.Count);
        Assert.All(defaults, item => Assert.True(item.Enabled));

        var existingReport = Guid.NewGuid();
        await service.CreateFaultStatusAsync(user, existingReport, "Treadmill", "UnderReview");
        await service.UpdatePreferencesAsync(user, [new("FaultReportStatusUpdate", false)]);
        var disabledReport = Guid.NewGuid();
        await service.CreateFaultStatusAsync(user, disabledReport, "Exercise bike", "UnderReview");
        var whileDisabled = await service.GetAsync(user, generateMemberNotifications: false);
        Assert.Contains(whileDisabled, item => item.Message.Contains("Treadmill", StringComparison.Ordinal));
        Assert.DoesNotContain(whileDisabled, item => item.Message.Contains("Exercise bike", StringComparison.Ordinal));

        await service.UpdatePreferencesAsync(user, [new("FaultReportStatusUpdate", true)]);
        var enabledReport = Guid.NewGuid();
        await service.CreateFaultStatusAsync(user, enabledReport, "Cable machine", "Resolved");
        await service.CreateFaultStatusAsync(user, enabledReport, "Cable machine", "Resolved");
        var afterEnabled = await service.GetAsync(user, generateMemberNotifications: false);
        Assert.Single(afterEnabled, item => item.Message.Contains("Cable machine", StringComparison.Ordinal));

        var stored = await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().NotificationPreferences
            .SingleAsync(item => item.UserId == user && item.Category == NotificationType.FaultReportStatusUpdate);
        Assert.True(stored.Enabled);
        await Assert.ThrowsAsync<CommandValidationException>(() => service.UpdatePreferencesAsync(user, [new("Unsupported", false)]));
    }

    private async Task<Guid>AddUser(string email,string role){await using var scope=factory.Services.CreateAsyncScope();var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();if(!await roles.RoleExistsAsync(role))Assert.True((await roles.CreateAsync(new IdentityRole<Guid>(role))).Succeeded);var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=new ApplicationUser{Id=Guid.NewGuid(),UserName=email,Email=email,FirstName="Stage",LastName="Eight"};Assert.True((await users.CreateAsync(user)).Succeeded);Assert.True((await users.AddToRoleAsync(user,role)).Succeeded);return user.Id;}
}
