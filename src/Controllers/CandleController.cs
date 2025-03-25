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
