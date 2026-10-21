using FitHub.Application.Common;
using FitHub.Application.Payments;
using FitHub.Application.Workouts;
using FitHub.Domain.Workouts;

namespace FitHub.UnitTests;

public sealed class WorkoutRestAndPaymentAmountTests
{
    [Fact]
    public void NullRestTimeRemainsValidForHistoricalSets()
    {
        var command = Command(restSeconds: null);

        WorkoutValidators.Validate(command);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3601)]
    public void RestTimeOutsideSupportedRangeIsRejected(int restSeconds)
    {
        var exception = Assert.Throws<CommandValidationException>(() =>
            WorkoutValidators.Validate(Command(restSeconds)));

        Assert.Contains("restSeconds", exception.Errors.Keys);
    }

    [Fact]
    public void ExerciseDurationAndRestRemainSeparateWithoutChangingVolume()
    {
        var set = new SetLog
        {
            Repetitions = 8,
            WeightKg = 60m,
            DurationSeconds = 45,
            RestSeconds = 90
        };

        Assert.Equal(45, set.DurationSeconds);
        Assert.Equal(90, set.RestSeconds);
        Assert.Equal(480m, set.Volume);
    }

    [Fact]
    public void MonthlyPriceConvertsToStripeMinorUnits()
    {
        Assert.Equal(2990, PaymentAmount.ToMinorUnits(29.90m));
    }

    private static LogExerciseCommand Command(int? restSeconds) =>
        new(Guid.NewGuid(), null, [new WorkoutSetCommand(8, 60m, 45, restSeconds)]);
}
