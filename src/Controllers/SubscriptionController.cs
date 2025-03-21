using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;

namespace Retsuko.Controllers;

[Route("subscription")]
public class SubscriptionController : Controller {
  [HttpPost("callback")]
  public async Task<IActionResult> Callback() {
    await Subscriber.HandleCallbackFromWorker();

    return Ok();
  }
}
