namespace Retsuko.Core;

public class Backtester: Trader {
  private readonly BacktestConfig config;

  public Backtester(BacktestConfig config): base(
    new BacktestCandleLoader(config.dataset),
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;
    this.metrics = TraderMetrics.Empty;
  }

  public TraderReport GetReport() {
    return new TraderReport(
      config,
      trades.ToArray(),
      metrics
    );
  }
}
