using FitHub.Application.Attendance;

namespace FitHub.UnitTests;

public sealed class MembershipCostCalculatorTests
{
    [Theory]
    [InlineData(49.90, 5, 9.98)]
    [InlineData(10.00, 3, 3.33)]
    public void CalculatesRoundedCostPerVisit(decimal price, int visits, decimal expected) =>
        Assert.Equal(expected, MembershipCostCalculator.Calculate(price, visits));

    [Fact]
    public void ReturnsNullWhenThereAreNoVisits() =>
        Assert.Null(MembershipCostCalculator.Calculate(49.90m, 0));
}
