using Microsoft.AspNetCore.Identity;

namespace FitHub.Domain.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public FitnessGoal FitnessGoal { get; private set; } = FitnessGoal.GeneralFitness;
    public ActivityLevel ActivityLevel { get; private set; } = ActivityLevel.Moderate;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public void MarkUpdated(DateTimeOffset timestamp) => UpdatedAt = timestamp;
    public void UpdateRecommendationPreferences(FitnessGoal goal, ActivityLevel activityLevel, DateTimeOffset timestamp){FitnessGoal=goal;ActivityLevel=activityLevel;MarkUpdated(timestamp);}
}
