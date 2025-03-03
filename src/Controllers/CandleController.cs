using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;

namespace Retsuko.Controllers;

[Route("candle")]
public class CandleController : Controller {
  [HttpGet]
  public async Task<IActionResult> GetDataset() {

    var tracer = MyTracer.Tracer;
    using var datasetSpan = tracer.StartActiveSpan("Downloader.GetDataset");
    var command = Database.Candle.CreateCommand();

    command.CommandText = "SELECT market, symbol, interval, min(ts), max(ts), count(ts) FROM candle GROUP BY market, symbol, interval";
    var reader = await command.ExecuteReaderAsync();
    var result = new List<Dataset>();
    while (reader.Read()) {
      var market = reader.GetString(0);
      var symbol = reader.GetString(1);
      var interval = reader.GetInt32(2);
      var start = reader.GetDateTime(3);
      var end = reader.GetDateTime(4);
      var count = reader.GetInt32(5);

      var dataset = new Dataset(Enum.Parse<Market>(market), symbol, interval, start, end, count);
      result.Add(dataset);
    }
    datasetSpan.End();

    using var exchangeSpan = tracer.StartActiveSpan("Broker.GetExchangeInfo");
    var info = await Broker.API.ExchangeData.GetExchangeInfoAsync();
    var symbols = info.Data.Symbols.ToArray();
    result.Sort((a, b) => {
      if (a.market != b.market) {
        return a.market == Market.futures ? -1 : 1;
      }

      var symbolA = Array.FindIndex(symbols, s => s.Name == a.symbol);
      var symbolB = Array.FindIndex(symbols, s => s.Name == b.symbol);

      if (symbolA != symbolB) {
        return symbolA - symbolB;
      }

      return a.interval - b.interval;
    });
    exchangeSpan.End();

    return Ok(result);
  }

  [HttpGet("market")]
  public async Task<IActionResult> GetMarkets() {
    var info = await Broker.API.ExchangeData.GetExchangeInfoAsync();

    return Ok(info.Data.Symbols);
  }

  [HttpPost("update")]
  public async Task<IActionResult> Update() {
    await Downloader.UpdateAll();

    return Ok();
  }
}
