using Itenium.Forge.Core;
using Refit;

namespace Shell.Api.Clients;

/// <summary>HTTP client for the FeatureFlags microservice.</summary>
public interface IFeatureFlagsClient
{
    /// <summary>Returns a paginated list of feature flags.</summary>
    [Get("/api/flags")]
    Task<ForgePagedResult<Flag>> GetFlagsAsync(ForgePageQuery query);
}

