using Refit;

namespace Shell.Api.Clients;

public interface IFeatureFlagsClient
{
    [Get("/api/flags")]
    Task<Flag[]> GetFlagsAsync();
}

