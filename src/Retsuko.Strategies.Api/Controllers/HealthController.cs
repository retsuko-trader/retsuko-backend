using Microsoft.AspNetCore.Mvc;

namespace Retsuko.Strategies.Controllers;

[Route("health")]
public class HealthController : Controller {
  [HttpGet()]
  public async Task<IActionResult> HealthCheck() {
    await ValueTask.CompletedTask;
    return Ok("ok");
  }
}
