using FitHub.Application.Analytics;
using FitHub.Application.Common;
using FitHub.Application.Workouts;
using FitHub.Application.Trainers;
using FitHub.Domain.Identity;
using FitHub.Domain.Workouts;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Workouts;

internal sealed class WorkoutService(
    FitHubDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ITrainerMemberAccessService trainerMemberAccess,
    TimeProvider timeProvider) : IWorkoutService
{
    public async Task<IReadOnlyCollection<ExerciseDto>> SearchExercisesAsync(string? search, string? muscleGroup, string? equipment, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Exercises.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(term) || (item.Instructions != null && item.Instructions.ToLower().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(muscleGroup)) query = query.Where(item => item.MuscleGroup == muscleGroup.Trim());
        if (!string.IsNullOrWhiteSpace(equipment)) query = query.Where(item => item.Equipment == equipment.Trim());
        return await query.OrderBy(item => item.Name).Select(MapExerciseExpression).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutTemplateDto>> GetTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var assignedIds = await dbContext.WorkoutPlanAssignments.AsNoTracking()
            .Where(item => item.MemberUserId == userId && item.IsActive)
            .Select(item => item.WorkoutTemplateId).ToListAsync(cancellationToken);
        var templates = await dbContext.WorkoutTemplates.AsNoTracking().Include(item => item.Exercises)
            .Where(item => (item.OwnerUserId == userId && item.Kind == WorkoutTemplateKind.Personal)
                || (item.IsPublic && item.Kind == WorkoutTemplateKind.Personal) || assignedIds.Contains(item.Id))
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);
        return templates.Select(item => MapTemplate(item, assignedIds.Contains(item.Id))).ToList();
    }

    public async Task<WorkoutTemplateDto> CreateTemplateAsync(Guid userId, UpsertWorkoutTemplateCommand command, CancellationToken cancellationToken = default)
    {
        WorkoutValidators.Validate(command);
        var exercises = await LoadExercisesAsync(command.ExerciseIds, cancellationToken);
        var template = new WorkoutTemplate
        {
            OwnerUserId = userId,
            Name = command.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            IsPublic = command.IsPublic,
            Kind = WorkoutTemplateKind.Personal
        };
        foreach (var exercise in exercises) template.Exercises.Add(exercise);
        dbContext.WorkoutTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTemplate(template, false);
    }

    public async Task<WorkoutTemplateDto?> UpdateTemplateAsync(Guid userId, Guid templateId, UpsertWorkoutTemplateCommand command, CancellationToken cancellationToken = default)
    {
        WorkoutValidators.Validate(command);
        var template = await dbContext.WorkoutTemplates.Include(item => item.Exercises)
            .SingleOrDefaultAsync(item => item.Id == templateId && item.OwnerUserId == userId
                && item.Kind == WorkoutTemplateKind.Personal, cancellationToken);
        if (template is null) return null;
        var exercises = await LoadExercisesAsync(command.ExerciseIds, cancellationToken);
        template.Name = command.Name.Trim();
        template.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        template.IsPublic = command.IsPublic;
        template.Exercises.Clear();
        foreach (var exercise in exercises) template.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTemplate(template, false);
    }

    public async Task<WorkoutSessionDto> StartSessionAsync(Guid userId, Guid? templateId, CancellationToken cancellationToken = default)
    {
        WorkoutTemplate? template = null;
        if (templateId is not null)
        {
            var assigned = await dbContext.WorkoutPlanAssignments.AnyAsync(item => item.WorkoutTemplateId == templateId && item.MemberUserId == userId && item.IsActive, cancellationToken);
            template = await dbContext.WorkoutTemplates.Include(item => item.Exercises)
                .SingleOrDefaultAsync(item => item.Id == templateId && (item.OwnerUserId == userId || item.IsPublic || assigned), cancellationToken)
                ?? throw Validation("templateId", "Workout template is unavailable.");
        }
        var session = new WorkoutSession { UserId = userId, WorkoutTemplate = template, StartedAt = timeProvider.GetUtcNow() };
        session.Start(timeProvider.GetUtcNow());
        var order = 0;
        foreach (var exercise in template?.Exercises.OrderBy(item => item.Name) ?? Enumerable.Empty<Exercise>())
            session.ExerciseLogs.Add(new ExerciseLog { Exercise = exercise, OrderIndex = order++ });
        dbContext.WorkoutSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSessionAsync(userId, session.Id, cancellationToken))!;
    }

    public async Task<WorkoutSessionDto?> LogExerciseAsync(Guid userId, Guid sessionId, LogExerciseCommand command, CancellationToken cancellationToken = default)
    {
        WorkoutValidators.Validate(command);
        var session = await dbContext.WorkoutSessions.Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets)
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (session is null) return null;
        if (session.Status != WorkoutSessionStatus.InProgress) throw Validation("session", "Only an active session can be changed.");
        if (!await dbContext.Exercises.AnyAsync(item => item.Id == command.ExerciseId, cancellationToken)) throw Validation("exerciseId", "Exercise was not found.");
        var log = session.ExerciseLogs.SingleOrDefault(item => item.ExerciseId == command.ExerciseId);
        if (log is null)
        {
            log = new ExerciseLog { ExerciseId = command.ExerciseId, OrderIndex = session.ExerciseLogs.Count };
            session.ExerciseLogs.Add(log);
        }
        else dbContext.SetLogs.RemoveRange(log.Sets);
        log.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        var setNumber = 1;
        foreach (var set in command.Sets)
            log.Sets.Add(new SetLog { SetNumber = setNumber++, Repetitions = set.Repetitions, WeightKg = set.WeightKg, DurationSeconds = set.DurationSeconds, RestSeconds = set.RestSeconds });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSessionAsync(userId, sessionId, cancellationToken);
    }

    public async Task<WorkoutSessionDto?> CompleteSessionAsync(Guid userId, Guid sessionId, string? notes, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.WorkoutSessions.Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets)
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (session is null) return null;
        if (!session.ExerciseLogs.SelectMany(item => item.Sets).Any()) throw Validation("sets", "Log at least one set before completing the workout.");
        session.Complete(timeProvider.GetUtcNow(), notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSessionAsync(userId, sessionId, cancellationToken);
    }

    public async Task<WorkoutSessionDto?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.WorkoutSessions.AsNoTracking().Include(item => item.WorkoutTemplate)
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Exercise)
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets)
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (session is null) return null;
        return await MapSessionAsync(session, cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutHistoryDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await dbContext.WorkoutSessions.AsNoTracking().Include(item => item.WorkoutTemplate)
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets)
            .Where(item => item.UserId == userId).OrderByDescending(item => item.StartedAt).Take(100).ToListAsync(cancellationToken);
        return sessions.Select(item => new WorkoutHistoryDto(item.Id, item.WorkoutTemplate?.Name ?? "Free workout", item.Status, item.StartedAt, item.EndedAt, Volume(item))).ToList();
    }

    public async Task<IReadOnlyCollection<WorkoutTemplateDto>> GetTrainerPlansAsync(
        Guid trainerUserId, CancellationToken cancellationToken = default)
    {
        var plans = await dbContext.WorkoutTemplates.AsNoTracking().Include(item => item.Exercises)
            .Where(item => item.OwnerUserId == trainerUserId && item.Kind == WorkoutTemplateKind.TrainerPlan)
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);
        return plans.Select(item => MapTemplate(item, false)).ToList();
    }

    public async Task<WorkoutTemplateDto> CreateTrainerPlanAsync(
        Guid trainerUserId, UpsertWorkoutTemplateCommand command, CancellationToken cancellationToken = default)
    {
        WorkoutValidators.Validate(command);
        var exercises = await LoadExercisesAsync(command.ExerciseIds, cancellationToken);
        var plan = new WorkoutTemplate
        {
            OwnerUserId = trainerUserId,
            Name = command.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            IsPublic = false,
            Kind = WorkoutTemplateKind.TrainerPlan
        };
        foreach (var exercise in exercises) plan.Exercises.Add(exercise);
        dbContext.WorkoutTemplates.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTemplate(plan, false);
    }

    public async Task<WorkoutTemplateDto?> UpdateTrainerPlanAsync(
        Guid trainerUserId, Guid templateId, UpsertWorkoutTemplateCommand command, CancellationToken cancellationToken = default)
    {
        WorkoutValidators.Validate(command);
        var plan = await dbContext.WorkoutTemplates.Include(item => item.Exercises).SingleOrDefaultAsync(item =>
            item.Id == templateId && item.OwnerUserId == trainerUserId && item.Kind == WorkoutTemplateKind.TrainerPlan,
            cancellationToken);
        if (plan is null) return null;
        var exercises = await LoadExercisesAsync(command.ExerciseIds, cancellationToken);
        plan.Name = command.Name.Trim();
        plan.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        plan.IsPublic = false;
        plan.Exercises.Clear();
        foreach (var exercise in exercises) plan.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTemplate(plan, false);
    }

    public async Task<IReadOnlyCollection<WorkoutAssignmentDto>> GetTrainerAssignmentsAsync(
        Guid trainerUserId, CancellationToken cancellationToken = default) =>
        await dbContext.WorkoutPlanAssignments.AsNoTracking().Where(item => item.TrainerUserId == trainerUserId
                && dbContext.TrainerMemberAssignments.Any(relation => relation.TrainerId == trainerUserId
                    && relation.MemberId == item.MemberUserId && relation.Status == TrainerMemberAssignmentStatus.Active))
            .Include(item => item.WorkoutTemplate).Include(item => item.MemberUser)
            .OrderByDescending(item => item.AssignedAt)
            .Select(item => new WorkoutAssignmentDto(item.Id, item.WorkoutTemplateId, item.WorkoutTemplate.Name,
                item.MemberUserId, (item.MemberUser.PreferredName ?? (item.MemberUser.FirstName + " " + item.MemberUser.LastName)).Trim(),
                item.AssignedAt, item.EndedAt, item.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<WorkoutAssignmentDto> AssignTemplateAsync(Guid trainerUserId, Guid templateId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        await trainerMemberAccess.EnsureAssignedAsync(trainerUserId, memberUserId, cancellationToken);
        var template = await dbContext.WorkoutTemplates.SingleOrDefaultAsync(item => item.Id == templateId
            && item.OwnerUserId == trainerUserId && item.Kind == WorkoutTemplateKind.TrainerPlan, cancellationToken)
            ?? throw Validation("templateId", "Workout template is unavailable for assignment.");
        var member = await userManager.FindByIdAsync(memberUserId.ToString());
        if (member is null || !member.IsActive || !await userManager.IsInRoleAsync(member, "Member")) throw Validation("memberUserId", "Member was not found.");
        var existing = await dbContext.WorkoutPlanAssignments.FirstOrDefaultAsync(item => item.WorkoutTemplateId == templateId && item.MemberUserId == memberUserId && item.IsActive, cancellationToken);
        if (existing is not null) return new(existing.Id, template.Id, template.Name, member.Id, $"{member.FirstName} {member.LastName}", existing.AssignedAt, existing.EndedAt, true);
        var assignment = new WorkoutPlanAssignment(templateId, memberUserId, trainerUserId, timeProvider.GetUtcNow());
        dbContext.WorkoutPlanAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(assignment.Id, template.Id, template.Name, member.Id, $"{member.FirstName} {member.LastName}", assignment.AssignedAt, assignment.EndedAt, true);
    }

    public async Task<bool> RemoveAssignmentAsync(Guid trainerUserId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.WorkoutPlanAssignments.SingleOrDefaultAsync(item =>
            item.Id == assignmentId && item.TrainerUserId == trainerUserId && item.IsActive, cancellationToken);
        if (assignment is null) return false;
        await trainerMemberAccess.EnsureAssignedAsync(trainerUserId, assignment.MemberUserId, cancellationToken);
        assignment.Deactivate(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<WorkoutSessionDto> MapSessionAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        var exerciseDtos = new List<WorkoutExerciseDto>();
        foreach (var log in session.ExerciseLogs.OrderBy(item => item.OrderIndex))
        {
            var previous = await dbContext.ExerciseLogs.AsNoTracking().Include(item => item.WorkoutSession).Include(item => item.Sets)
                .Where(item => item.ExerciseId == log.ExerciseId && item.Sets.Any() && item.WorkoutSession.UserId == session.UserId && item.WorkoutSession.Status == WorkoutSessionStatus.Completed && item.WorkoutSession.StartedAt < session.StartedAt)
                .OrderByDescending(item => item.WorkoutSession.EndedAt).FirstOrDefaultAsync(cancellationToken);
            var priorWeights = await dbContext.SetLogs.AsNoTracking()
                .Where(set => set.ExerciseLog.ExerciseId == log.ExerciseId && set.ExerciseLog.WorkoutSession.UserId == session.UserId && set.ExerciseLog.WorkoutSession.Status == WorkoutSessionStatus.Completed && set.ExerciseLog.WorkoutSession.StartedAt < session.StartedAt && set.WeightKg != null)
                .Select(set => set.WeightKg!.Value).ToListAsync(cancellationToken);
            decimal? previousBest = priorWeights.Count == 0 ? null : priorWeights.Max();
            var sets = log.Sets.OrderBy(item => item.SetNumber).Select(set => new WorkoutSetDto(
                set.Id, set.SetNumber, set.Repetitions, set.WeightKg, set.DurationSeconds, set.RestSeconds, set.Volume,
                set.WeightKg is not null && AnalyticsRules.IsPersonalRecord(set.WeightKg.Value, previousBest))).ToList();
            var previousDto = previous is null ? null : new PreviousPerformanceDto(
                previous.WorkoutSession.EndedAt ?? previous.WorkoutSession.StartedAt,
                previous.Sets.Select(set => set.WeightKg ?? 0m).DefaultIfEmpty(0m).Max(),
                AnalyticsRules.WorkoutVolume(previous.Sets.Select(set => (set.Repetitions, set.WeightKg))));
            exerciseDtos.Add(new WorkoutExerciseDto(log.Id, MapExercise(log.Exercise), log.Notes, previousDto, sets));
        }
        return new WorkoutSessionDto(session.Id, session.WorkoutTemplateId, session.WorkoutTemplate?.Name, session.Status, session.StartedAt, session.EndedAt, session.Notes, Volume(session), exerciseDtos);
    }

    private async Task<List<Exercise>> LoadExercisesAsync(IReadOnlyCollection<Guid> exerciseIds, CancellationToken cancellationToken)
    {
        var exercises = await dbContext.Exercises.Where(item => exerciseIds.Contains(item.Id)).ToListAsync(cancellationToken);
        if (exercises.Count != exerciseIds.Count) throw Validation("exerciseIds", "One or more exercises were not found.");
        return exerciseIds.Select(id => exercises.Single(item => item.Id == id)).ToList();
    }

    private static decimal Volume(WorkoutSession session) => AnalyticsRules.WorkoutVolume(session.ExerciseLogs.SelectMany(item => item.Sets).Select(set => (set.Repetitions, set.WeightKg)));
    private static readonly System.Linq.Expressions.Expression<Func<Exercise, ExerciseDto>> MapExerciseExpression = item => new(item.Id, item.Name, item.MuscleGroup, item.Equipment, item.Instructions);
    private static ExerciseDto MapExercise(Exercise item) => new(item.Id, item.Name, item.MuscleGroup, item.Equipment, item.Instructions);
    private static WorkoutTemplateDto MapTemplate(WorkoutTemplate item, bool assigned) => new(item.Id, item.Name, item.Description, item.IsPublic, item.OwnerUserId, item.Exercises.Select(MapExercise).ToList(), assigned, item.Kind);
    private static CommandValidationException Validation(string key, string message) => new(new Dictionary<string, string[]> { [key] = [message] });
}
