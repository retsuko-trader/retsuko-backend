using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Retsuko.Core;
using Retsuko.Dtos;

namespace Retsuko.Controllers;

[Route("backtest")]
public class BacktestController: Controller {
  [HttpGet("config")]
  public async Task<IActionResult> GetConfig([FromQuery]string strategy) {
    var config = await StrategyLoader.GetDefaultConfig(strategy);
    if (config == null) {
      return NotFound();
    }

    return Ok(new SingleBacktestRunRequest(new BacktestConfig(
      new DatasetConfig(Market.futures, 0, Binance.Net.Enums.KlineInterval.EightHour, DateTime.Parse("2021-01-01"), DateTime.Parse("2021-01-31")),
      new StrategyConfig(strategy, config),
      new PaperBrokerConfig(1000, 0.001, false, false)
    )));
  }

  public record SingleBacktestRunRequest(
    [Required] BacktestConfig config,
    bool hideTrades = false
  );

  [HttpPost("single/run")]
  public async Task<IActionResult> RunSingle([FromBody]SingleBacktestRunRequest req) {
    var tracer = MyTracer.Tracer;

    var loader = new BacktestCandleLoader(req.config.dataset);
    var backtester = new Backtester(req.config);

    await backtester.Init();

    using (var preload = tracer.StartActiveSpan("Backtester.Preload")) {
      await backtester.Preload(loader);
    }

    var run = tracer.StartActiveSpan("Backtester.Run");
    while (await loader.Read()) {
      await backtester.Tick(await loader.LoadOne());
    }
    run.End();

    await backtester.ProcessSignals();
    await backtester.FinalizeMetrics();

    var report = backtester.GetReport();
    if (req.hideTrades) {
      return Ok(report with { trades = [] });
    }

    return Ok(report);
  }

  [HttpGet("bulk/run")]
  public async Task<IActionResult> GetBulkRuns() {
    var runs = await BacktestRun.List();

    return Ok(runs.Select(x => new ExtBacktestRun(x)));
  }

  [HttpGet("bulk/run/{id}")]
  public async Task<IActionResult> GetBulkRun(string id) {
    var run = await BacktestRun.Get(id);
    var singles = await BacktestSingle.List(id);

    ExtBacktestRun? extRun = run.HasValue ? new ExtBacktestRun(run.Value) : null;

    var singleIds = singles.Select(x => x.id).ToArray();
    var allTrades = await BacktestTrade.List(singleIds);

    return Ok(new {
      run = extRun,
      singles = singles.Select(x => new ExtBacktestSingle(x, allTrades.Where(t => t.single_id == x.id).ToArray())),
    });
  }

  [HttpGet("bulk/run/{id}/{singleID}/trades")]
  public async Task<IActionResult> GetSingleTrades(string id, string singleId) {
    var trades = await BacktestTrade.List(singleId);

    return Ok(trades);
  }

  [HttpPost("bulk/run")]
  public async Task<IActionResult> RunBulk([FromBody]BulkBacktestConfig config) {
    var bulk = new BulkBacktester(config);

    _ = Task.Run(() => bulk.Run());
    await ValueTask.CompletedTask;

    return Ok(bulk.BacktestRun);
  }
}
