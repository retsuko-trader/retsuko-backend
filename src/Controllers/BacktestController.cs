using Microsoft.AspNetCore.Mvc;

[Route("backtest")]
public class BacktestController: Controller {
  [HttpGet("config")]
  public IActionResult GetConfig() {
    return Ok(new BacktestConfig(
      new DatasetConfig(Market.futures, "BTCUSDT", 1, DateTime.Parse("2021-01-01"), DateTime.Parse("2021-01-31")),
      new StrategyConfig("SuperTrend", SuperTrendStrategy.DefaultConfig),
      new PaperBrokerConfig(1000, 0.001, false, false)
    ));
  }

  public record SingleBacktestRunRequest(BacktestConfig config);

  [HttpPost("single/run")]
  public async Task<IActionResult> RunSingle([FromBody]SingleBacktestRunRequest req) {
    var backtester = new Backtester(req.config);

    await backtester.Init();
    while (!backtester.IsEnded) {
      await backtester.Tick();
    }

    return Ok(backtester.GetReport());
  }
}
