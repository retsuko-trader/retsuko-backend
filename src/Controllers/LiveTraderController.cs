using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;
using Retsuko.Dtos;

namespace Retsuko.Controllers;

[Route("livetrader")]
public class LiveTraderController : Controller {
  [HttpGet]
  public async Task<IActionResult> GetList() {
    var result = await LiveTraderState.List();
    return Ok(result.Select(x => new ExtLiveTraderState(x)));
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> Get(string id) {
    var state = await LiveTraderState.Get(id);
    if (state == null) {
      return NotFound();
    }

    return Ok(new ExtLiveTraderState(state.Value));
  }

  [HttpGet("{id}/trade")]
  public async Task<IActionResult> GetTrades(string id) {
    var trades = await LiveTraderTrade.List(id);
    return Ok(trades);
  }

  public record CreateLiveTraderRequest(
    [Required] LiveTraderConfig config
  );

  [HttpPost]
  public async Task<IActionResult> Create([FromBody]CreateLiveTraderRequest req) {
    var symbol = await Symbol.Get(req.config.dataset.symbolId);
    if (symbol == null) {
      return BadRequest("Invalid symbol");
    }

    var loader = new PreloadCandleLoader(req.config.dataset);
    var trader = LiveTrader.Create(req.config);

    using (var preload = MyTracer.Tracer.StartActiveSpan("LiveTrader.Preload")) {
      await trader.Preload(loader);
    }

    var state = trader.Serialize();

    state.Insert();
    await Subscriber.Subscribe(trader.Id, symbol.Value.name, req.config.dataset.interval);

    return Ok(new ExtLiveTraderState(state));
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(string id) {
    var trader = await LiveTrader.Load(id);
    if (trader == null) {
      return NotFound();
    }
    var state = trader.Serialize();
    state.endedAt = DateTime.Now;
    await state.Update();

    await Subscriber.UnSubscribe(trader.Id);

    return Ok();
  }
}
