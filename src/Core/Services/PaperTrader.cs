namespace Retsuko.Core;

using Record = Retsuko.PaperTrader;

public class PaperTrader: Trader {
  private readonly PapertraderConfig config;

  private Record entity;

  private PaperTrader(PapertraderConfig config, string id): base(
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;

    entity = new Record(
      id: id,
      name: config.info.name,
      description: config.info.description,
      created_at: DateTime.Now,
      updated_at: DateTime.Now,
      ended_at: null,
      symbolId: config.dataset.symbolId,
      interval: (int)config.dataset.interval,
      strategy_name: config.strategy.name,
      strategy_config: config.strategy.config,
      strategy_state: strategy.Serialize(),
      broker_state: broker.Serialize(),
      metrics: metrics.ToString()
    );
  }

  public static PaperTrader Create(PapertraderConfig config) {
    var id = new Visus.Cuid.Cuid2().ToString();
    return new PaperTrader(config, id);
  }
}
