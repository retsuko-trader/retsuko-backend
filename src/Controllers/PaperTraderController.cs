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

  [HttpGet("{id}")]
  public async Task<IActionResult> Get(string id) {
    var state = await PaperTraderState.Get(id);
    if (state == null) {
      return NotFound();
    }

    return Ok(new ExtPaperTraderState(state.Value));
  }

  [HttpGet("{id}/trade")]
  public async Task<IActionResult> GetTrades(string id) {
    var trades = await PaperTraderTrade.List(id);
    return Ok(trades);
  }

  public record CreatePaperTraderRequest(
    [Required] PapertraderConfig config
  );

  [HttpPost]
  public async Task<IActionResult> Create([FromBody]CreatePaperTraderRequest req) {
    var symbol = await Symbol.Get(req.config.dataset.symbolId);
    if (symbol == null) {
      return BadRequest("Invalid symbol");
    }

    var loader = new PreloadCandleLoader(req.config.dataset);
    using var trader = PaperTrader.Create(req.config);

    using (var preload = MyTracer.Tracer.StartActiveSpan("PaperTrader.Preload")) {
      await trader.Preload(loader);
    }

    var state = await trader.Serialize();

    state.Insert();
    await Subscriber.Subscribe(trader.Id, symbol.Value.name, req.config.dataset.interval);

    return Ok(new ExtPaperTraderState(state));
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(string id) {
    using var trader = await PaperTrader.Load(id);
    if (trader == null) {
      return NotFound();
    }
    var state = await trader.Serialize();
    state.endedAt = DateTime.Now;
    await state.Update();

    await Subscriber.UnSubscribe(trader.Id);

    return Ok();
  }

  [HttpPost("{id}/check")]
  public async Task<IActionResult> Check(string id) {
    var state = await PaperTraderState.Get(id);
    if (!state.HasValue) {
      return NotFound();
    }

    var checker = new StrategyCheck(
      state.Value.strategy_name,
      state.Value.strategy_config,
      state.Value.strategy_state
    );

    var result = await checker.Check();
    return Ok(result);
  }

  [HttpGet("{id}/dump")]
  public async Task<IActionResult> Dump(string id) {
    var state = await PaperTraderState.Get(id);
    if (!state.HasValue) {
      return NotFound();
    }

    var checker = new StrategyCheck(
      state.Value.strategy_name,
      state.Value.strategy_config,
      state.Value.strategy_state
    );

    var result = await checker.Dump();
    return Ok(result);
  }
}
