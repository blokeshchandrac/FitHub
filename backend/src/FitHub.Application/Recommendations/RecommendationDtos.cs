namespace FitHub.Application.Recommendations;
public sealed record RecommendationDto(Guid Id,string RuleCode,string Title,string Content,string Category,string Reason,DateTimeOffset CreatedAt);
public sealed record RecommendationOverviewDto(string FitnessGoal,string ActivityLevel,string NutritionDisclaimer,IReadOnlyCollection<RecommendationDto> Recommendations);
public sealed record UpdateRecommendationPreferencesCommand(string FitnessGoal,string ActivityLevel);
