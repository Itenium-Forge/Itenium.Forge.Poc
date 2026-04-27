using Itenium.Forge.Core;
using Microsoft.AspNetCore.Mvc;
using Shell.Api.Clients;

namespace Shell.Api.Controllers;

/// <summary>Proxy controller for feature flags from the FeatureFlags microservice.</summary>
[ApiController]
[Route("api/[controller]")]
public class FlagsController(IFeatureFlagsClient featureFlagsClient) : ControllerBase
{
    /// <summary>Returns a paginated list of feature flags.</summary>
    [HttpGet]
    [ProducesResponseType<ForgePagedResult<Flag>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] ForgePageQuery query)
    {
        var flags = await featureFlagsClient.GetFlagsAsync(query);
        return Ok(flags);
    }
}
