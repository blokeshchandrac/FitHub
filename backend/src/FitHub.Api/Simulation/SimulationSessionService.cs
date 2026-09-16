using System.Collections.Concurrent;

namespace FitHub.Api.Simulation;

/// <summary>
/// Tracks only virtual-entry eligibility. Persisted equipment state is handled by IEquipmentService.
/// </summary>
public sealed class SimulationSessionService
{
    private const int MaximumSessions = 100;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> enteredMembers = new(StringComparer.Ordinal);

    public bool TryEnter(string sessionId, Guid memberId)
    {
        if (!IsValidSession(sessionId)) return false;
        if (!enteredMembers.ContainsKey(sessionId) && enteredMembers.Count >= MaximumSessions) return false;
        enteredMembers.GetOrAdd(sessionId, _ => new ConcurrentDictionary<Guid, byte>())[memberId] = 0;
        return true;
    }

    public bool HasEntered(string sessionId, Guid memberId) =>
        enteredMembers.TryGetValue(sessionId, out var members) && members.ContainsKey(memberId);

    public void Reset(string sessionId) => enteredMembers.TryRemove(sessionId, out _);

    public static SimulationMemberSnapshot? ApplyOverride(SimulationMemberSnapshot? member, SimulationStatusOverride statusOverride)
    {
        if (member is null || statusOverride == SimulationStatusOverride.Actual) return member;
        return statusOverride switch
        {
            SimulationStatusOverride.ActiveMembership => member with { MembershipStatus = "ACTIVE", CardStatus = "ACTIVE", AccountStatus = "ACTIVE" },
            SimulationStatusOverride.ExpiredMembership => member with { MembershipStatus = "EXPIRED", AccountStatus = "ACTIVE" },
            SimulationStatusOverride.BlockedCard => member with { CardStatus = "BLOCKED", AccountStatus = "ACTIVE" },
            SimulationStatusOverride.SuspendedAccount => member with { AccountStatus = "SUSPENDED" },
            _ => member
        };
    }

    public static (string Code, string Message)? ValidateMember(SimulationMemberSnapshot? member)
    {
        if (member is null) return ("INVALID_CARD", "Access denied — NFC card is invalid.");
        if (member.AccountStatus != "ACTIVE") return ("USER_DISABLED", "Access denied — the Member account is suspended.");
        if (member.MembershipStatus == "EXPIRED") return ("MEMBERSHIP_EXPIRED", "Access denied — membership expired.");
        if (member.MembershipStatus != "ACTIVE") return ("MEMBERSHIP_INACTIVE", "Access denied — an active membership is required.");
        if (member.CardStatus != "ACTIVE") return ("CARD_BLOCKED", "Access denied — NFC card blocked.");
        return null;
    }

    public static bool IsValidSession(string sessionId) =>
        !string.IsNullOrWhiteSpace(sessionId) && sessionId.Length <= 64 && sessionId.StartsWith("SIM-", StringComparison.Ordinal);
}
