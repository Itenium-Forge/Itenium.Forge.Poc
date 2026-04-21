using Microsoft.AspNetCore.Mvc;

namespace FeatureFlags.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlagsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new[]
    {
        new { Name = "dark-mode",     Enabled = true  },
        new { Name = "new-dashboard", Enabled = false },
        new { Name = "beta-export",   Enabled = false },
    });
}
