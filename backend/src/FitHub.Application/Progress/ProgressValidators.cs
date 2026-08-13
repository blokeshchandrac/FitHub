using FitHub.Application.Common;

namespace FitHub.Application.Progress;

public static class ProgressValidators
{
    public static void Validate(BodyMeasurementCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (AllValuesMissing(command)) errors["measurement"] = ["Provide at least one measurement."];
        Range(command.WeightKg, 20m, 400m, "weightKg", "Weight", errors);
        Range(command.HeightCm, 100m, 250m, "heightCm", "Height", errors);
        Range(command.BodyFatPercentage, 2m, 70m, "bodyFatPercentage", "Body-fat percentage", errors);
        Range(command.ChestCm, 30m, 300m, "chestCm", "Chest", errors);
        Range(command.WaistCm, 30m, 300m, "waistCm", "Waist", errors);
        Range(command.HipCm, 30m, 300m, "hipCm", "Hip", errors);
        if (command.Notes?.Length > 1000) errors["notes"] = ["Notes must not exceed 1000 characters."];
        if (errors.Count > 0) throw new CommandValidationException(errors);
    }

    private static bool AllValuesMissing(BodyMeasurementCommand command) =>
        command.WeightKg is null && command.HeightCm is null && command.BodyFatPercentage is null && command.ChestCm is null && command.WaistCm is null && command.HipCm is null;

    private static void Range(decimal? value, decimal minimum, decimal maximum, string key, string label, Dictionary<string, string[]> errors)
    {
        if (value is not null && (value < minimum || value > maximum || decimal.Round(value.Value, 2) != value))
            errors[key] = [$"{label} must be between {minimum} and {maximum} with at most two decimals."];
    }
}
