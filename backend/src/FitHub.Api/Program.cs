using FitHub.Infrastructure;
using FitHub.Infrastructure.Persistence.Seeding;
using FitHub.Api.Infrastructure;
using FitHub.Api.Realtime;
using FitHub.Api.Simulation;
using FitHub.Application.Equipment;
using FitHub.Infrastructure.Payments;
using FitHub.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<CommandValidationExceptionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)));
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key.TrimStart('$', '.').Replace("request.", string.Empty, StringComparison.OrdinalIgnoreCase),
                entry => entry.Value!.Errors.Select(_ => "The supplied value is invalid.").ToArray());
        var details = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request"
        };
        details.Extensions["code"] = "validation_failed";
        details.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(details);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<PasswordResetOptions>()
    .Bind(builder.Configuration.GetSection(PasswordResetOptions.SectionName))
    .Validate(options => options.ExpiryMinutes is >= 5 and <= 1440,
        "PasswordReset:ExpiryMinutes must be between 5 and 1440 minutes.")
    .Validate(options => Uri.TryCreate(options.FrontendBaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https",
        "PasswordReset:FrontendBaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SmtpHost), "Email:SmtpHost is required.")
    .Validate(options => options.SmtpPort is > 0 and <= 65535, "Email:SmtpPort is invalid.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SenderAddress), "Email:SenderAddress is required.")
    .ValidateOnStart();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddOptions<StripeOptions>()
        .Bind(builder.Configuration.GetSection(StripeOptions.SectionName))
        .Validate(options => StripeOptions.IsTestSecretKey(options.SecretKey),
            "Stripe:SecretKey must be a real Stripe Test Mode key beginning with sk_test_.")
        .Validate(options => StripeOptions.IsWebhookSecret(options.WebhookSecret),
            "Stripe:WebhookSecret must be a Stripe CLI or Test Mode signing secret beginning with whsec_.")
        .Validate(options => StripeOptions.IsHttpUrl(options.WebBaseUrl),
            "Stripe:WebBaseUrl must be an absolute HTTP or HTTPS URL.")
        .Validate(options => StripeOptions.IsHttpUrl(options.CheckoutSuccessUrl),
            "Stripe:CheckoutSuccessUrl must be an absolute HTTP or HTTPS URL.")
        .Validate(options => StripeOptions.IsHttpUrl(options.CheckoutCancelUrl),
            "Stripe:CheckoutCancelUrl must be an absolute HTTP or HTTPS URL.")
        .ValidateOnStart();
}
builder.Services.AddSignalR();
builder.Services.AddOptions<SimulationOptions>()
    .Bind(builder.Configuration.GetSection(SimulationOptions.SectionName))
    .Validate(options => options.ClearanceThresholdMetres is > 0 and <= 10,
        "Simulation:ClearanceThresholdMetres must be between 0 and 10 metres.")
    .Validate(options => options.ClearanceSeconds is >= 1 and <= 60,
        "Simulation:ClearanceSeconds must be between 1 and 60 seconds.")
    .Validate(options => options.ParkingScanSeconds is >= 5 and <= 300,
        "Simulation:ParkingScanSeconds must be between 5 and 300 seconds.")
    .Validate(options => options.ParkingDemoScanSeconds is >= 1 and <= 30,
        "Simulation:ParkingDemoScanSeconds must be between 1 and 30 seconds.")
    .ValidateOnStart();
builder.Services.AddSingleton<SimulationSessionService>();
builder.Services.AddScoped<SimulationMemberCatalogService>();
builder.Services.AddScoped<ConnectedEquipmentSimulationService>();
builder.Services.AddScoped<IEquipmentRealtimeNotifier, SignalREquipmentNotifier>();
builder.Services.AddScoped<FitHub.Application.Parking.IParkingRealtimeNotifier, SignalRParkingNotifier>();
builder.Services.AddHostedService<EquipmentOfflineMonitor>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("password-recovery", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Environment.IsEnvironment("Testing") ? 100 : 3,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("simulation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 180,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedData:Enabled"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
        }
        return Task.CompletedTask;
    });
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
var faultPhotoPath = Path.GetFullPath(builder.Configuration["FaultPhotos:LocalPath"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "fault-images"));
Directory.CreateDirectory(faultPhotoPath);
app.MapControllers();
app.MapHub<EquipmentHub>("/hubs/equipment");
app.MapHub<ParkingHub>("/hubs/parking");
app.MapGet("/fault-images/{fileName}", (string fileName) =>
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (Path.GetFileName(fileName) != fileName || extension is not (".jpg" or ".png" or ".webp"))
        {
            return Results.NotFound();
        }

        var fullPath = Path.Combine(faultPhotoPath, fileName);
        if (!File.Exists(fullPath))
        {
            return Results.NotFound();
        }

        var contentType = extension switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            _ => "image/webp"
        };
        return Results.File(fullPath, contentType);
    })
    .RequireAuthorization(
        new AuthorizeAttribute
        {
            Roles = $"{FitHub.Application.Authentication.AuthorizationPolicies.Member},{FitHub.Application.Authentication.AuthorizationPolicies.Administrator}"
        },
        new AuthorizeAttribute { Policy = FitHub.Application.Authentication.AuthorizationPolicies.ActiveMembership });
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("Health")
    .WithTags("Health");

app.Run();

public partial class Program;
