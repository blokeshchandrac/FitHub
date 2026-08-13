using System.Security.Claims;
using System.Text;
using FitHub.Application.Authentication;
using FitHub.Application.Attendance;
using FitHub.Application.Memberships;
using FitHub.Application.Payments;
using FitHub.Application.Progress;
using FitHub.Application.Dashboard;
using FitHub.Application.Workouts;
using FitHub.Application.Equipment;
using FitHub.Application.FaultReports;
using FitHub.Application.Parking;
using FitHub.Application.Recommendations;
using FitHub.Application.Administration;
using FitHub.Application.Bookings;
using FitHub.Application.Notifications;
using FitHub.Infrastructure.AttendanceServices;
using FitHub.Infrastructure.Authentication;
using FitHub.Infrastructure.Authorization;
using FitHub.Infrastructure.Memberships;
using FitHub.Infrastructure.Payments;
using FitHub.Infrastructure.Progress;
using FitHub.Infrastructure.Dashboard;
using FitHub.Infrastructure.Workouts;
using FitHub.Infrastructure.EquipmentServices;
using FitHub.Infrastructure.FaultReports;
using FitHub.Infrastructure.ParkingServices;
using FitHub.Infrastructure.Recommendations;
using FitHub.Infrastructure.Administration;
using FitHub.Infrastructure.Bookings;
using FitHub.Infrastructure.Notifications;
using FitHub.Domain.Parking;
using FitHub.Domain.Equipment;
using FitHub.Infrastructure.Persistence;
using FitHub.Infrastructure.Persistence.Seeding;
using FitHub.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FitHubDatabase")
            ?? throw new InvalidOperationException("Connection string 'FitHubDatabase' is not configured.");

        services.AddDbContext<FitHubDbContext>(options => options.UseNpgsql(connectionString));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<FitHubDbContext>()
            .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        services.Configure<JwtOptions>(jwtSection);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<JwtTokenIssuer>();
        services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
        services.AddScoped<IWorkoutService, WorkoutService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IEquipmentAccessService, EquipmentAccessService>();
        services.AddScoped<ICardUidHasher, CardUidHasher>();
        services.AddScoped<IDeviceAuthenticator, DeviceAuthenticator>();
        services.AddScoped<IFaultReportService, FaultReportService>();
        services.AddScoped<IPhotoStorage, LocalPhotoStorage>();
        services.AddScoped<IParkingService, ParkingService>();
        services.AddScoped<IParkingCameraAuthenticator, ParkingCameraAuthenticator>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IAdministratorDashboardService, AdministratorDashboardService>();
        services.AddScoped<IClassBookingService, ClassBookingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPasswordHasher<EquipmentDevice>, PasswordHasher<EquipmentDevice>>();
        services.AddScoped<IPasswordHasher<ParkingCamera>, PasswordHasher<ParkingCamera>>();
        services.AddScoped<IAuthorizationHandler, ActiveMembershipHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, MembershipAuthorizationResultHandler>();
        services.AddScoped<IAuthorizationHandler, CheckedInEquipmentHandler>();
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                    {
                        var signingKey = configuration[$"{JwtOptions.SectionName}:SigningKey"];
                        return Encoding.UTF8.GetByteCount(signingKey ?? string.Empty) >= 32
                            ? [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!))]
                            : [];
                    },
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrWhiteSpace(token) &&
                            (path.StartsWithSegments("/hubs/equipment") || path.StartsWithSegments("/hubs/parking")))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Member, policy => policy.RequireRole(AuthorizationPolicies.Member))
            .AddPolicy(AuthorizationPolicies.Trainer, policy => policy.RequireRole(AuthorizationPolicies.Trainer))
            .AddPolicy(AuthorizationPolicies.Administrator, policy => policy.RequireRole(AuthorizationPolicies.Administrator))
            .AddPolicy(AuthorizationPolicies.Staff, policy => policy.RequireRole(AuthorizationPolicies.Trainer, AuthorizationPolicies.Administrator))
            .AddPolicy(AuthorizationPolicies.ActiveMembership, policy => policy.AddRequirements(new ActiveMembershipRequirement()));
        services.AddAuthorizationBuilder().AddPolicy(AuthorizationPolicies.CheckedInEquipment, policy => policy.AddRequirements(new CheckedInEquipmentRequirement()));
        services.AddScoped<DevelopmentDataSeeder>();
        return services;
    }
}
