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
      broker.InitialBalance,
      broker.GetPortfolio().currency + broker.GetPortfolio().asset * lastCandle!.Value.close,
      metrics.totalProfit,
      trades.ToArray(),
      metrics
    );
  }
}
