namespace FitHub.Application.Authentication;

public static class AuthorizationPolicies
{
    public const string Member = nameof(Member);
    public const string Trainer = nameof(Trainer);
    public const string Administrator = nameof(Administrator);
    public const string Staff = nameof(Staff);
    public const string ActiveMembership = nameof(ActiveMembership);
    public const string CheckedInEquipment = nameof(CheckedInEquipment);
}
