using Itenium.Forge.Controllers;
using Itenium.Forge.HealthChecks;
using Itenium.Forge.HttpClients;
using Itenium.Forge.Logging;
using Itenium.Forge.SecurityHeaders;
using Itenium.Forge.Settings;
using Serilog;
using Shell.Api;
using Shell.Api.Clients;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddForgeSettings<ShellSettings>();
    builder.AddForgeLogging();

    builder.AddForgeControllers();
    builder.AddForgeProblemDetails();
    builder.AddForgeHealthChecks();

    builder.AddForgeHttpClient<IFeatureFlagsClient>("FeatureFlags");

    var app = builder.Build();

    app.UseForgeSecurityHeaders();
    app.UseForgeProblemDetails();
    app.UseForgeLogging();
    app.UseForgeControllers();
    app.UseForgeHealthChecks();

    app.MapGet("/", () => "Hello World");

    app.MapGet("/apps", (IConfiguration config) =>
        config.GetSection("Apps")
              .GetChildren()
              .Select(s => new { Name = s.Key, RemoteUrl = s["RemoteUrl"] })
              .ToArray());

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

#pragma warning disable S1118 // needed for WebApplicationFactory<Program> in tests
public partial class Program { }
