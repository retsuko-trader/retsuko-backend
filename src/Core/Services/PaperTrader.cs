namespace Retsuko.Core;

public class PaperTrader: Trader {
  private readonly PapertraderConfig config;

  public PaperTrader(PapertraderConfig config): base(
    new PapertraderCandleLoader(config.dataset),
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;
    this.metrics = TraderMetrics.Empty;
  }
}
