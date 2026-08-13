using FitHub.Application.Parking;using Microsoft.AspNetCore.SignalR;
namespace FitHub.Api.Realtime;
public sealed class SignalRParkingNotifier(IHubContext<ParkingHub> hub):IParkingRealtimeNotifier{public Task ParkingChangedAsync(ParkingChangedDto change,CancellationToken ct=default)=>hub.Clients.All.SendAsync("parkingChanged",change,ct);}
