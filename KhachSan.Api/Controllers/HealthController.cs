using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { success = true, message = "Healthy" });
    }
}