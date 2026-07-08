using Microsoft.AspNetCore.Mvc;

namespace Retsuko.Controllers;

[Route("portfolio")]
public class PortfolioController: Controller {
  [HttpGet]
  public async Task<IActionResult> Get() {
    var portfolio = await PortfolioService.Get();
    return Ok(portfolio);
  }
}
