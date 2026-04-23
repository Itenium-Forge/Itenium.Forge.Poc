using Refit;

namespace Shell.Api.Clients;

/// <summary>HTTP client for the FeatureFlags microservice.</summary>
public interface IFeatureFlagsClient
{
    /// <summary>Returns all feature flags.</summary>
    [Get("/api/flags")]
    Task<Flag[]> GetFlagsAsync();
}

