using Shell.Api.Clients;

namespace Shell.Api.Tests;

public class MockFeatureFlagsClient : IFeatureFlagsClient
{
    public Task<Flag[]> GetFlagsAsync() => Task.FromResult(new[]
    {
        new Flag("dark-mode", true),
        new Flag("new-dashboard", false),
    });
}
