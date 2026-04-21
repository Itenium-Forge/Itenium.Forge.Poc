using FeatureFlags.Api;
using Itenium.Forge.Controllers;
using Itenium.Forge.HealthChecks;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Serilog;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddForgeSettings<FeatureFlagsSettings>();
    builder.AddForgeLogging();

    builder.AddForgeControllers();
    builder.AddForgeProblemDetails();
    builder.AddForgeHealthChecks();

    var app = builder.Build();

    app.UseForgeProblemDetails();
    app.UseForgeLogging();
    app.UseForgeControllers();
    app.UseCors("CorsPolicy"); // TODO: remove when Forge.Controllers > 0.3.13 is released
    app.UseForgeHealthChecks();

    app.MapGet("/", () => "Hello from Feature Flags");

    app.MapGet("/flags", () => new[]
    {
        new { Name = "dark-mode",     Enabled = true  },
        new { Name = "new-dashboard", Enabled = false },
        new { Name = "beta-export",   Enabled = false },
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
