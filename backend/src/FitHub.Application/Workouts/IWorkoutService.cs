namespace FitHub.Application.Workouts;

public interface IWorkoutService
{
    Task<IReadOnlyCollection<ExerciseDto>> SearchExercisesAsync(string? search, string? muscleGroup, string? equipment, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkoutTemplateDto>> GetTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WorkoutTemplateDto> CreateTemplateAsync(Guid userId, UpsertWorkoutTemplateCommand command, CancellationToken cancellationToken = default);
    Task<WorkoutTemplateDto?> UpdateTemplateAsync(Guid userId, Guid templateId, UpsertWorkoutTemplateCommand command, CancellationToken cancellationToken = default);
    Task<WorkoutSessionDto> StartSessionAsync(Guid userId, Guid? templateId, CancellationToken cancellationToken = default);
    Task<WorkoutSessionDto?> LogExerciseAsync(Guid userId, Guid sessionId, LogExerciseCommand command, CancellationToken cancellationToken = default);
    Task<WorkoutSessionDto?> CompleteSessionAsync(Guid userId, Guid sessionId, string? notes, CancellationToken cancellationToken = default);
    Task<WorkoutSessionDto?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkoutHistoryDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MemberSummaryDto>> GetMembersAsync(CancellationToken cancellationToken = default);
    Task<WorkoutAssignmentDto> AssignTemplateAsync(Guid trainerUserId, Guid templateId, Guid memberUserId, CancellationToken cancellationToken = default);
}
