using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;

namespace Retsuko.Controllers;

[Route("test")]
public class TestController : Controller {
  [HttpPost("callback")]
  public async Task<IActionResult> Callback() {
    throw new Exception("test exception");
  }
}
