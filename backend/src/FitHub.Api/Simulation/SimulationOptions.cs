namespace FitHub.Api.Simulation;

public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";
    public bool Enabled { get; init; }
    public decimal ClearanceThresholdMetres { get; init; } = 2.2m;
    public int ClearanceSeconds { get; init; } = 30;
    public int ParkingScanSeconds { get; init; } = 60;
    public int ParkingDemoScanSeconds { get; init; } = 5;
}
