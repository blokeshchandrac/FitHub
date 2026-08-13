using System.Reflection;
using FitHub.Api.Controllers;
using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace FitHub.IntegrationTests;

public sealed class ApiSecurityMetadataTests
{
    [Fact]
    public void EveryControllerActionHasExplicitAuthorizationDecision()
    {
        var controllerTypes = typeof(Program).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

        foreach (var controllerType in controllerTypes)
        {
            var classHasDecision = controllerType.IsDefined(typeof(AuthorizeAttribute), true) ||
                                   controllerType.IsDefined(typeof(AllowAnonymousAttribute), true);
            foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(true).Any()))
            {
                var methodHasDecision = method.IsDefined(typeof(AuthorizeAttribute), true) ||
                                        method.IsDefined(typeof(AllowAnonymousAttribute), true);
                Assert.True(classHasDecision || methodHasDecision,
                    $"{controllerType.Name}.{method.Name} has no explicit authorization decision.");
            }
        }
    }

    [Theory]
    [InlineData(nameof(AuthenticationController.Login), "login")]
    [InlineData(nameof(AuthenticationController.ForgotPassword), "password-recovery")]
    public void SensitiveAnonymousEndpointsHaveExpectedRateLimit(string actionName, string policyName)
    {
        var action = typeof(AuthenticationController).GetMethod(actionName);
        var rateLimit = Assert.Single(action!.GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Equal(policyName, rateLimit.PolicyName);
    }

    [Theory]
    [InlineData(typeof(AdministratorDashboardController))]
    [InlineData(typeof(FaultReportsAdministrationController))]
    [InlineData(typeof(MembershipPlansAdministrationController))]
    public void AdministrationControllersRequireAdministratorPolicy(Type controllerType)
    {
        var authorize = Assert.Single(controllerType.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.Administrator, authorize.Policy);
    }
}
