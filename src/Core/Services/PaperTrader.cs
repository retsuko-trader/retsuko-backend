using System.Text.Json;

namespace Retsuko.Core;

public class PaperTrader: Trader, ISerializable<PaperTraderState> {
  private readonly PapertraderConfig config;

  private PaperTraderState state;

  private PaperTrader(PapertraderConfig config, string id): base(
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;

    state = new PaperTraderState(
      id: id,
      name: config.info.name,
      description: config.info.description,
      created_at: DateTime.Now,
      updated_at: DateTime.Now,
      ended_at: null,
      dataset: JsonSerializer.Serialize(config.dataset),
      strategy_name: config.strategy.name,
      strategy_config: config.strategy.config,
      strategy_state: strategy.Serialize(),
      broker_config: JsonSerializer.Serialize(config.broker),
      broker_state: broker.Serialize(),
      metrics: JsonSerializer.Serialize(metrics)
    );
  }

  public override async Task<Trade?> Tick(Candle candle) {
    var result = await base.Tick(candle);

    state.updated_at = DateTime.Now;
    state.strategy_state = strategy.Serialize();
    state.broker_state = broker.Serialize();
    return result;
  }

  protected override void ProcessMetrics(Candle candle, Trade? trade) {
    base.ProcessMetrics(candle, trade);

    state.metrics = JsonSerializer.Serialize(metrics);
  }

  public static PaperTrader Create(PapertraderConfig config) {
    var id = new Visus.Cuid.Cuid2().ToString();
    return new PaperTrader(config, id);
  }

  public static async Task<PaperTrader?> Load(string id) {
    var state = await PaperTraderState.Get(id);
    if (state == null) {
      return null;
    }

    var config = state.Value.Config;
    var trader = new PaperTrader(config, id);
    trader.Deserialize(state.Value);

    return trader;
  }

  public PaperTraderState Serialize() {
    return state;
  }

  public void Deserialize(PaperTraderState state) {
    this.state = state;

    strategy.Deserialize(state.strategy_state);
    broker.Deserialize(state.broker_state);
    metrics = JsonSerializer.Deserialize<TraderMetrics>(state.metrics);
  }
}
