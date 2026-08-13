namespace FitHub.Application.Attendance;

public static class MembershipCostCalculator
{
    public static decimal? Calculate(decimal membershipCost, int visits) =>
        visits <= 0 ? null : decimal.Round(membershipCost / visits, 2, MidpointRounding.AwayFromZero);
}
