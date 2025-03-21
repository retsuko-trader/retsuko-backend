using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;

namespace Retsuko.Controllers;

[Route("strategy")]
public class StrategyController: Controller {
  [HttpGet]
  public async Task<IActionResult> GetStrategies() {
    var result = StrategyLoader.GetStrategyEntries();
    return Ok(result);
  }
}
