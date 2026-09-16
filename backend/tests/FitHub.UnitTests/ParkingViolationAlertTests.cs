using FitHub.Domain.Parking;

namespace FitHub.UnitTests;

public sealed class ParkingViolationAlertTests
{
    [Fact]
    public void RepeatedObservationKeepsOneActiveAlertAndResolutionCanBeReactivated()
    {
        var first = DateTimeOffset.UtcNow;
        var alert = new ParkingViolationAlert("SIM-1", "Car 01", "P-A2,P-A3", "TWO_SPACE_OBSTRUCTION",
            "SIM-PARK-CAMERA-01", "Obstructing two spaces.", "SIM-1:Car 01:P-A2|P-A3", first);

        alert.Observe(first.AddSeconds(5), "Still obstructing two spaces.");
        Assert.True(alert.IsActive);
        Assert.Equal(first.AddSeconds(5), alert.LastObservedAt);

        alert.Resolve(first.AddSeconds(10));
        Assert.False(alert.IsActive);
        Assert.Equal(first.AddSeconds(10), alert.ResolvedAt);

        alert.Observe(first.AddSeconds(15), "Obstruction returned.");
        Assert.True(alert.IsActive);
        Assert.Null(alert.ResolvedAt);
        Assert.Equal(first.AddSeconds(15), alert.DetectedAt);
    }
}
