using FitHub.Domain.Common;
using FitHub.Domain.Identity;
namespace FitHub.Domain.Equipment;
public sealed class MemberAccessCard : Entity
{
    private MemberAccessCard() { }
    public MemberAccessCard(Guid userId, string uidHash) { UserId = userId; UidHash = uidHash; }
    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public string UidHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public void Block(DateTimeOffset timestamp)
    {
        IsActive = false;
        MarkUpdated(timestamp);
    }

    public void Unblock(DateTimeOffset timestamp)
    {
        IsActive = true;
        MarkUpdated(timestamp);
    }
}
