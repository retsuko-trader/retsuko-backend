using Microsoft.AspNetCore.Mvc;
using Retsuko.Strategies.Services;

namespace Retsuko.Strategies.Controllers;

[Route("strategy")]
public class StrategyController: Controller {
  [HttpGet]
  public async Task<IActionResult> GetStrategies() {
    var result = StrategyLoader.GetStrategyEntries();
    return await ValueTask.FromResult(Ok(result));
  }
}
