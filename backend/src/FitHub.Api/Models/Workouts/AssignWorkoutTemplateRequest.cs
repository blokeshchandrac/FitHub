namespace FitHub.Api.Models.Workouts;

public sealed record AssignWorkoutTemplateRequest(Guid TemplateId, Guid MemberUserId);
