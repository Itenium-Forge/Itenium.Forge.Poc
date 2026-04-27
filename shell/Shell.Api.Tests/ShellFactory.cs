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
                var registration = options.Registrations.FirstOrDefault(r => string.Equals(r.Name, "http-FeatureFlags", StringComparison.Ordinal));
                if (registration != null)
                    options.Registrations.Remove(registration);

                var otlp = options.Registrations.FirstOrDefault(r => string.Equals(r.Name, "otlp", StringComparison.Ordinal));
                if (otlp != null)
                    options.Registrations.Remove(otlp);
            });
        });
    }
}

