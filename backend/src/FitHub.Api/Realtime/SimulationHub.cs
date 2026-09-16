using Microsoft.AspNetCore.SignalR;

namespace FitHub.Api.Realtime;

/// <summary>Publishes fictional virtual-lab events only. The hub is mapped only when Simulation mode is enabled.</summary>
public sealed class SimulationHub : Hub;
