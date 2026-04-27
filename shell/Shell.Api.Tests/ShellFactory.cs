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
                // Remove all health checks except 'self' to avoid external dependencies during tests
                var toRemove = options.Registrations
                    .Where(r => !string.Equals(r.Name, "self", StringComparison.Ordinal))
                    .ToList();

                foreach (var registration in toRemove)
                {
                    options.Registrations.Remove(registration);
                }
            });
        });
    }
}

