using FitHub.Application.Common;
using FitHub.Application.Profiles;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Profiles;

internal sealed class ProfileService(FitHubDbContext db, TimeProvider timeProvider) : IProfileService
{
    public async Task<MemberProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.PreferredGymBranch).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyCollection<GymBranchDto>> GetActiveBranchesAsync(CancellationToken cancellationToken = default) =>
        await db.GymBranches.AsNoTracking().Where(branch => branch.IsActive).OrderBy(branch => branch.Name)
            .Select(branch => new GymBranchDto(branch.Id, branch.Code, branch.Name)).ToListAsync(cancellationToken);

    public async Task<MemberProfileDto?> UpdateAsync(Guid userId, UpdateMemberProfileCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);
        var branch = await db.GymBranches.SingleOrDefaultAsync(x => x.Id == command.PreferredGymBranchId && x.IsActive, cancellationToken);
        if (branch is null)
            throw new CommandValidationException(new Dictionary<string, string[]> { ["preferredGymBranchId"] = ["Select an active gym branch."] });
        var user = await db.Users.Include(x => x.PreferredGymBranch).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return null;
        user.UpdateProfile(command.PreferredName, command.DateOfBirth, command.HeightCm, command.WeightKg,
            command.FitnessGoal, command.ActivityLevel, command.ExperienceLevel, command.PlannedTrainingDays,
            branch.Id, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Map(user, branch.Name);
    }

    private void Validate(UpdateMemberProfileCommand command)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var age = today.Year - command.DateOfBirth.Year;
        if (command.DateOfBirth > today.AddYears(-age)) age--;
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.PreferredName) || command.PreferredName.Trim().Length > 50)
            errors["preferredName"] = ["Preferred name must contain between 1 and 50 characters."];
        if (age is < 13 or > 100) errors["dateOfBirth"] = ["Date of birth must represent an age between 13 and 100."];
        if (command.HeightCm is < 80m or > 250m) errors["heightCm"] = ["Height must be between 80 and 250 cm."];
        if (command.WeightKg is < 25m or > 350m) errors["weightKg"] = ["Weight must be between 25 and 350 kg."];
        if (!Enum.IsDefined(command.FitnessGoal)) errors["fitnessGoal"] = ["Select a valid fitness goal."];
        if (!Enum.IsDefined(command.ActivityLevel)) errors["activityLevel"] = ["Select a valid activity level."];
        if (!Enum.IsDefined(command.ExperienceLevel)) errors["experienceLevel"] = ["Select a valid experience level."];
        if (command.PlannedTrainingDays.Count == 0) errors["plannedTrainingDays"] = ["Select at least one planned training day."];
        else if (command.PlannedTrainingDays.Any(day => !Enum.IsDefined(day))) errors["plannedTrainingDays"] = ["Select only valid weekdays."];
        else if (command.PlannedTrainingDays.Distinct().Count() != command.PlannedTrainingDays.Count) errors["plannedTrainingDays"] = ["Each planned training day may be selected only once."];
        if (command.PreferredGymBranchId == Guid.Empty) errors["preferredGymBranchId"] = ["Select an active gym branch."];
        if (errors.Count > 0) throw new CommandValidationException(errors);
    }

    private static MemberProfileDto Map(Domain.Identity.ApplicationUser user, string? branchName = null) => new(
        user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.PreferredName,
        user.DateOfBirth, user.HeightCm, user.WeightKg, user.FitnessGoal, user.ActivityLevel,
        user.ExperienceLevel, user.GetPlannedTrainingDays(), user.PreferredGymBranchId,
        branchName ?? user.PreferredGymBranch?.Name, user.OnboardingCompleted);
}
