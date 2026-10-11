
using Microsoft.AspNetCore.Mvc;

namespace PaperFlow.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            success = true,
            message = "PaperFlow Backend is running!",
            timestamp = DateTime.UtcNow
        });
    }
}
