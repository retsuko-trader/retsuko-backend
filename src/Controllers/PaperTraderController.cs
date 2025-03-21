using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;
using Retsuko.Dtos;

namespace Retsuko.Controllers;

[Route("papertrader")]
public class PaperTraderController : Controller {
  [HttpGet]
  public async Task<IActionResult> GetList() {
    var result = await PaperTraderState.List();
    return Ok(result.Select(x => new ExtPaperTraderState(x)));
  }

  public record CreatePaperTraderRequest(
    [Required] PapertraderConfig config
  );

  [HttpPost("create")]
  public async Task<IActionResult> Create([FromBody]CreatePaperTraderRequest req) {
    var symbol = await Symbol.Get(req.config.dataset.symbolId);
    if (symbol == null) {
      return BadRequest("Invalid symbol");
    }

    var loader = new PapertraderCandleLoader(req.config.dataset);
    var trader = PaperTrader.Create(req.config);

    using (var preload = MyTracer.Tracer.StartActiveSpan("PaperTrader.Preload")) {
      await trader.Preload(loader);
    }

    var state = trader.Serialize();

    state.Insert();
    await Subscriber.Subscribe(trader.Id, symbol.Value.name, req.config.dataset.interval);

    return Ok(new ExtPaperTraderState(state));
  }
}
