namespace Retsuko.Core;

public class BulkBacktester {
  private readonly BulkBacktestConfig config;
  private readonly IReadOnlyList<BacktestConfig> singleConfigs;
  private readonly BacktestRun run;

  public BacktestRun BacktestRun => run;

  public BulkBacktester(BulkBacktestConfig config) {
    this.config = config;
    var singleConfigs = new List<BacktestConfig>();

    foreach (var dataset in config.datasets) {
      foreach (var strategy in config.strategies) {
        singleConfigs.Add(new BacktestConfig(dataset, strategy, config.broker));
      }
    }

    this.singleConfigs = singleConfigs;

    run = BacktestRun.Create(config);
  }

  public async Task Run() {
    MyLogger.Logger.LogInformation("Bulk backtest run started for runId={run.id}", run.id);
    var tracer = MyTracer.Tracer;
    using var rootSpan = tracer.StartActiveSpan("BulkBacktester.Run");
    rootSpan.SetAttribute("run.id", run.id);
    rootSpan.SetAttribute("run.name", run.name);
    rootSpan.SetAttribute("run.description", run.description);

    run.Insert();

    await Parallel.ForEachAsync(singleConfigs, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (config, t) => {
      using var span = tracer.StartActiveSpan("BulkBacktester.Single");
      MyLogger.Logger.LogInformation("Bulk backtest single run started for runId={run.id}", run.id);
      span.SetAttribute("dataset.symbolId", config.dataset.symbolId);
      span.SetAttribute("dataset.interval", (int)config.dataset.interval);
      span.SetAttribute("strategy", config.strategy.name);

      using var runSpan = tracer.StartActiveSpan("BulkBacktester.Single.Run");

      var loader = new BacktestCandleLoader(config.dataset);
      var backtester = new Backtester(config);

      await backtester.Init();
      await backtester.Preload(loader);

      await foreach (var chunk in loader.BatchLoad(200)) {
        await backtester.TickBulk(chunk);
      }

      runSpan.End();

      await backtester.ProcessSignals();
      await backtester.FinalizeMetrics();
      var report = await backtester.GetReport();
      var single = BacktestSingle.Create(run.id, config, report.metrics);
      single.Insert();

      var backtestTrades = report.trades.Select(trade => BacktestTrade.Create(single.id, trade));
      BacktestTrade.InsertBulk(backtestTrades);

      MyLogger.Logger.LogInformation("Bulk backtest single run ended for runId={run.id}, singleId={single.id}", run.id, single.id);
      span.End();
    });

    await run.UpdateEnd();
    MyLogger.Logger.LogInformation("Bulk backtest run ended for runId={run.id}", run.id);

    rootSpan.End();
  }
}
