using FitHub.Domain.Equipment;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Domain.Parking;
using FitHub.Domain.Payments;
using FitHub.Domain.Workouts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FitHub.Application.Equipment;

namespace FitHub.Infrastructure.Persistence.Seeding;

public sealed class DevelopmentDataSeeder(
    FitHubDbContext dbContext,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IPasswordHasher<EquipmentDevice> equipmentDevicePasswordHasher,
    IPasswordHasher<ParkingCamera> parkingCameraPasswordHasher,
    ICardUidHasher cardUidHasher,
    ILogger<DevelopmentDataSeeder> logger)
{
    private static readonly string[] Roles = ["Member", "Trainer", "Administrator"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedMembershipPlansAsync(cancellationToken);
        await SeedDemoMembershipsAsync(cancellationToken);
        await SeedExercisesAsync(cancellationToken);
        await SeedEquipmentAsync(cancellationToken);
        await SeedEquipmentDevicesAndCardAsync(cancellationToken);
        await SeedParkingSpacesAsync(cancellationToken);
        await SeedParkingCameraAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)), $"create role '{role}'");
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        var password = configuration["SeedData:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Demo users were not seeded because SeedData:DemoPassword is not configured.");
            return;
        }

        var accounts = configuration.GetSection("SeedData:Accounts").Get<DemoAccountOptions[]>() ?? [];
        foreach (var account in accounts)
        {
            if (!Roles.Contains(account.Role, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Development seed role '{account.Role}' is not supported.");
            }

            await EnsureUserAsync(account.Email, account.FirstName, account.LastName, account.Role, password);
        }
    }

    private async Task EnsureUserAsync(string email, string firstName, string lastName, string role, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            EnsureSucceeded(await userManager.CreateAsync(user, password), $"create demo user '{email}'");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"assign role '{role}' to '{email}'");
        }
    }

    private async Task SeedMembershipPlansAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.MembershipPlans.AnyAsync(cancellationToken)) return;

        dbContext.MembershipPlans.AddRange(
            new MembershipPlan { Name = "Monthly", Description = "Flexible monthly membership", Price = 49.90m, BillingPeriodMonths = 1 },
            new MembershipPlan { Name = "Annual", Description = "Twelve-month membership", Price = 499.00m, BillingPeriodMonths = 12 });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoMembershipsAsync(CancellationToken cancellationToken)
    {
        var accounts = configuration.GetSection("SeedData:Accounts").Get<DemoAccountOptions[]>() ?? [];
        var activeAccounts = accounts.Where(account =>
            account.Role == "Member" && account.MembershipState == "Active");
        var plan = await dbContext.MembershipPlans.OrderBy(item => item.Price).FirstOrDefaultAsync(cancellationToken);
        if (plan is null) return;

        foreach (var account in activeAccounts)
        {
            var user = await userManager.FindByEmailAsync(account.Email);
            if (user is null) continue;

            var now = DateTimeOffset.UtcNow;
            var membership = await dbContext.Memberships
                .OrderByDescending(item => item.EndsAt)
                .FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
            if (membership is null)
            {
                membership = new Membership(user.Id, plan.Id, now.AddDays(-7), now.AddMonths(plan.BillingPeriodMonths));
                membership.Activate(now.AddDays(-7), now.AddMonths(plan.BillingPeriodMonths));
                dbContext.Memberships.Add(membership);
            }
            else if (!membership.IsActiveAt(now))
            {
                membership.Activate(now.AddDays(-7), now.AddMonths(plan.BillingPeriodMonths));
            }

            var providerReference = $"demo-seed-{user.Id:N}";
            if (!await dbContext.Payments.AnyAsync(item => item.ProviderReference == providerReference, cancellationToken))
            {
                var payment = new Payment
                {
                    UserId = user.Id,
                    Membership = membership,
                    MembershipPlanId = plan.Id,
                    PurchaseKind = MembershipPurchaseKind.Purchase,
                    Amount = plan.Price,
                    Currency = "EUR",
                    ProviderReference = providerReference
                };
                payment.MarkSucceeded(now.AddDays(-7));
                dbContext.Payments.Add(payment);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedExercisesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Exercises.AnyAsync(cancellationToken)) return;

        dbContext.Exercises.AddRange(
            new Exercise { Name = "Back Squat", MuscleGroup = "Legs", Equipment = "Barbell", Instructions = "Keep the torso braced and descend under control." },
            new Exercise { Name = "Bench Press", MuscleGroup = "Chest", Equipment = "Barbell and bench", Instructions = "Maintain stable shoulder blades throughout the press." },
            new Exercise { Name = "Lat Pulldown", MuscleGroup = "Back", Equipment = "Cable machine", Instructions = "Pull toward the upper chest without swinging." });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEquipmentAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Equipment.AnyAsync(cancellationToken)) return;

        dbContext.Equipment.AddRange(
            new Equipment("TREAD-001", "Treadmill") { Location = "Cardio zone" },
            new Equipment("BIKE-001", "Exercise bike") { Location = "Cardio zone" },
            new Equipment("RACK-001", "Power rack") { Location = "Strength zone" });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEquipmentDevicesAndCardAsync(CancellationToken cancellationToken)
    {
        var credential = configuration["SeedData:EquipmentDeviceCredential"];
        if (!string.IsNullOrWhiteSpace(credential))
        {
            var equipment = await dbContext.Equipment.OrderBy(x => x.Code).ToListAsync(cancellationToken);
            foreach (var item in equipment.Where(item => !dbContext.EquipmentDevices.Any(device => device.EquipmentId == item.Id)))
            {
                var device = new EquipmentDevice(item.Id, $"SIM-{item.Code}", "pending");
                device.SetCredentialHash(equipmentDevicePasswordHasher.HashPassword(device, credential));
                dbContext.EquipmentDevices.Add(device);
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else logger.LogWarning("Equipment devices were not seeded because SeedData:EquipmentDeviceCredential is not configured.");

        var cardUid = configuration["SeedData:MemberCardUid"];
        var memberEmail = configuration.GetSection("SeedData:Accounts").Get<DemoAccountOptions[]>()?.FirstOrDefault(x => x.Role == "Member")?.Email;
        var member = string.IsNullOrWhiteSpace(memberEmail) ? null : await userManager.FindByEmailAsync(memberEmail);
        if (!string.IsNullOrWhiteSpace(cardUid) && member is not null)
        {
            var hash = cardUidHasher.Hash(cardUid);
            if (!await dbContext.MemberAccessCards.AnyAsync(x => x.UidHash == hash, cancellationToken)) dbContext.MemberAccessCards.Add(new MemberAccessCard(member.Id, hash));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedParkingSpacesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ParkingSpaces.AnyAsync(cancellationToken)) return;

        dbContext.ParkingSpaces.AddRange(
            new ParkingSpace("A-01", "A") { IsAccessible = true },
            new ParkingSpace("A-02", "A"),
            new ParkingSpace("B-01", "B"));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedParkingCameraAsync(CancellationToken cancellationToken)
    {
        var credential=configuration["SeedData:ParkingCameraCredential"];
        if(string.IsNullOrWhiteSpace(credential)){logger.LogWarning("Parking camera was not seeded because SeedData:ParkingCameraCredential is not configured.");return;}
        if(await dbContext.ParkingCameras.AnyAsync(cancellationToken))return;
        var camera=new ParkingCamera("SIM-PARKING-01","pending");camera.SetCredentialHash(parkingCameraPasswordHasher.HashPassword(camera,credential));dbContext.ParkingCameras.Add(camera);await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;

        throw new InvalidOperationException($"Failed to {operation}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }

    private sealed class DemoAccountOptions
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string MembershipState { get; init; } = "None";
    }
}
