using Microsoft.AspNetCore.Mvc;
using Shell.Api.Clients;

namespace Shell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlagsController(IFeatureFlagsClient featureFlagsClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var flags = await featureFlagsClient.GetFlagsAsync();
        return Ok(flags);
    }
}
