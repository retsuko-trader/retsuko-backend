using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;

namespace Retsuko.Controllers;

[Route("strategy")]
public class StrategyController: Controller {
  [HttpGet]
  public async Task<IActionResult> GetStrategies([FromQuery] bool dev = false) {
    var result = await StrategyLoader.GetStrategyEntries(dev);
    return Ok(result);
  }
}
