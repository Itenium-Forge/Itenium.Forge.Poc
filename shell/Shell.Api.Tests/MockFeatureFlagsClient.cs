using Itenium.Forge.Core;
using Shell.Api.Clients;

namespace Shell.Api.Tests;

public class MockFeatureFlagsClient : IFeatureFlagsClient
{
    public Task<ForgePagedResult<Flag>> GetFlagsAsync(ForgePageQuery query)
    {
        var items = new[]
        {
            new Flag("dark-mode", true),
            new Flag("new-dashboard", false),
        };

        return Task.FromResult(new ForgePagedResult<Flag>(items, items.Length, query.Page, query.PageSize));
    }
}
