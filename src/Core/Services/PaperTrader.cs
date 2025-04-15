using System.Text.Json;
using Retsuko.Plugins;

namespace Retsuko.Core;

public class PaperTrader: Trader, ISerializable<PaperTraderState> {
  public string Id => state.id;

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
      createdAt: DateTime.Now,
      updatedAt: DateTime.Now,
      endedAt: null,
      dataset: JsonSerializer.Serialize(config.dataset),
      strategy_name: config.strategy.name,
      strategy_config: config.strategy.config,
      strategy_state: strategy.Serialize(),
      broker_config: JsonSerializer.Serialize(config.broker),
      broker_state: broker.Serialize(),
      metrics: JsonSerializer.Serialize(metrics),
      states: JsonSerializer.Serialize(new InnerState(firstCandle, lastCandle))
    );
  }

  public override async Task Preload(ICandleLoader loader) {
    await base.Preload(loader);
    state.strategy_state = strategy.Serialize();
  }

  public override async Task<Trade?> Tick(Candle candle) {
    var trade = await base.Tick(candle);

    var delay = DateTime.Now - candle.ts;
    var delayed = delay > TimeSpan.FromHours(1);
    if (delayed)  {
      MyLogger.Logger.LogWarning(
        "PaperTrader {traderId} tick delayed {delay} for candle {candle}",
        Id,
        delay,
        candle
      );
    }

    if (trade.HasValue) {
      var id = new Visus.Cuid.Cuid2().ToString();

      var entity = new PaperTraderTrade(
        id: id,
        traderId: Id,
        ts: DateTime.Now,
        signal: trade.Value.signal,
        confidence: trade.Value.confidence,
        asset: trade.Value.asset,
        currency: trade.Value.currency,
        price: trade.Value.price,
        profit: trade.Value.profit
      );
      entity.Insert();
    }

    state.updatedAt = DateTime.Now;
    state.strategy_state = strategy.Serialize();
    state.broker_state = broker.Serialize();
    try {
      state.metrics = JsonSerializer.Serialize(metrics);
    } catch (Exception ex) {
      MyLogger.Logger.LogError(ex, "Failed to serialize metrics");
      EventDispatcher.Exception(null, ex);
    }
    state.states = JsonSerializer.Serialize(new InnerState(firstCandle, lastCandle));
    return trade;
  }

  public static PaperTrader Create(PapertraderConfig config) {
    var id = TraderIdHelper.GeneratePaperTraderId();
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
    var innerState = JsonSerializer.Deserialize<InnerState>(state.states)!;
    firstCandle = innerState.firstCandle;
    lastCandle = innerState.lastCandle;
  }

  record InnerState(Candle? firstCandle, Candle? lastCandle);
}
