using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shell.Api.Clients;

namespace Shell.Api.Tests;

public class ShellFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IFeatureFlagsClient>(new MockFeatureFlagsClient());

            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                var registration = options.Registrations.FirstOrDefault(r => r.Name == "http-FeatureFlags");
                if (registration != null)
                    options.Registrations.Remove(registration);
            });
        });
    }
}

public class MockFeatureFlagsClient : IFeatureFlagsClient
{
    public Task<Flag[]> GetFlagsAsync() => Task.FromResult(new[]
    {
        new Flag("dark-mode", true),
        new Flag("new-dashboard", false),
    });
}
