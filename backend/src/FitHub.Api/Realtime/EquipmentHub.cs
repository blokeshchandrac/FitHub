using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace FitHub.Api.Realtime;
// Live availability is read-only and is available to every active Member. Check-in is still
// enforced by the equipment action endpoints before a Member can claim, share, or release.
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
public sealed class EquipmentHub : Hub;
