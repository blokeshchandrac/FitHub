using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace FitHub.Api.Realtime;
[Authorize(Policy = AuthorizationPolicies.CheckedInEquipment)]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
public sealed class EquipmentHub : Hub;
