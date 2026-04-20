using Itenium.Forge.Controllers;
using Itenium.Forge.HealthChecks;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Serilog;
using Shell.Api;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddForgeSettings<ShellSettings>();
    builder.AddForgeLogging();

    builder.AddForgeControllers();
    builder.AddForgeProblemDetails();
    builder.AddForgeHealthChecks();

    var app = builder.Build();

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
