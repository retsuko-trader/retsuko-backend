namespace Retsuko.Core;

public class Backtester: Trader {
  private readonly BacktestConfig config;

  public Backtester(BacktestConfig config): base(
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;
  }

  public TraderReport GetReport() {
    return new TraderReport(
      config,
      trades.ToArray(),
      metrics
    );
  }
}
