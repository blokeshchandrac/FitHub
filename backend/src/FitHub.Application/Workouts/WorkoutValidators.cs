using FitHub.Application.Common;

namespace FitHub.Application.Workouts;

public static class WorkoutValidators
{
    public static void Validate(UpsertWorkoutTemplateCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 150) errors["name"] = ["Name is required and must not exceed 150 characters."];
        if (command.Description?.Length > 2000) errors["description"] = ["Description must not exceed 2000 characters."];
        if (command.ExerciseIds.Count is < 1 or > 50 || command.ExerciseIds.Distinct().Count() != command.ExerciseIds.Count) errors["exerciseIds"] = ["Choose between 1 and 50 distinct exercises."];
        if (errors.Count > 0) throw new CommandValidationException(errors);
    }

    public static void Validate(LogExerciseCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.ExerciseId == Guid.Empty) errors["exerciseId"] = ["Exercise is required."];
        if (command.Notes?.Length > 1000) errors["notes"] = ["Notes must not exceed 1000 characters."];
        if (command.Sets.Count is < 1 or > 30) errors["sets"] = ["Log between 1 and 30 sets."];
        if (command.Sets.Any(set => set.Repetitions is < 0 or > 1000)) errors["repetitions"] = ["Repetitions must be between 0 and 1000."];
        if (command.Sets.Any(set => set.WeightKg is < 0 or > 1000 || decimal.Round(set.WeightKg, 2) != set.WeightKg)) errors["weightKg"] = ["Weight must be between 0 and 1000 kg with at most two decimals."];
        if (command.Sets.Any(set => set.DurationSeconds is < 0 or > 86400)) errors["durationSeconds"] = ["Duration must be between 0 and 86400 seconds."];
        if (command.Sets.Any(set => set.RestSeconds is < 0 or > 3600)) errors["restSeconds"] = ["Rest time must be between 0 and 3600 seconds."];
        if (command.Sets.Any(set => set.Repetitions == 0 && set.WeightKg == 0 && (set.DurationSeconds ?? 0) == 0)) errors["sets"] = ["Each set must contain repetitions, weight, or duration."];
        if (errors.Count > 0) throw new CommandValidationException(errors);
    }
}
