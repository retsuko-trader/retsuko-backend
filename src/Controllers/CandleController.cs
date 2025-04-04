using Binance.Net.Enums;
using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;

namespace Retsuko.Controllers;

[Route("candle")]
public class CandleController : Controller {
  [HttpGet]
  public async Task<IActionResult> GetDataset() {
    var result = await Dataset.List();
    return Ok(result);
  }

  [HttpGet("candle/{market}/{symbolId}/{interval}")]
  public async Task<IActionResult> GetCandle(
    int market,
    int symbolId,
    KlineInterval interval,
    [FromQuery] DateTime? start,
    [FromQuery] DateTime? end,
    [FromQuery] float? sampleRate
  ) {
    var candles = await Candle.List(symbolId, interval, start, end, sampleRate);
    return Ok(candles);
  }

  [HttpGet("symbol")]
  public async Task<IActionResult> GetSymbols() {
    var result = await Symbol.List();
    return Ok(result);
  }

  [HttpGet("market")]
  public async Task<IActionResult> GetMarkets() {
    var info = await Exchanger.API.ExchangeData.GetExchangeInfoAsync();

    return Ok(info.Data.Symbols);
  }

  [HttpPost("update")]
  public async Task<IActionResult> Update() {
    await Downloader.UpdateAll();

    return Ok();
  }
}
