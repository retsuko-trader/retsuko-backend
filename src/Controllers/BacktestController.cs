using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

[Route("backtest")]
public class BacktestController: Controller {
  [HttpGet("config")]
  public IActionResult GetConfig([FromQuery]string strategy) {
    return Ok(new SingleBacktestRunRequest(new BacktestConfig(
      new DatasetConfig(Market.futures, "BTCUSDT", Binance.Net.Enums.KlineInterval.EightHour, DateTime.Parse("2021-01-01"), DateTime.Parse("2021-01-31")),
      new StrategyConfig(strategy, StrategyLoader.GetDefaultConfig(strategy)!),
      new PaperBrokerConfig(1000, 0.001, false, false)
    )));
  }

  public record SingleBacktestRunRequest(
    [Required] BacktestConfig config,
    bool hideTrades = false
  );

  [HttpPost("single/run")]
  public async Task<IActionResult> RunSingle([FromBody]SingleBacktestRunRequest req) {
    var backtester = new Backtester(req.config);

    await backtester.Init();
    while (!backtester.IsEnded) {
      await backtester.Tick();
    }

    var report = backtester.GetReport();
    if (req.hideTrades) {
      return Ok(report with { trades = [] });
    }

    return Ok(report);
  }
}
