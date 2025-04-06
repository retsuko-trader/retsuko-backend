using System.Text.Json;

namespace Retsuko.Core;

public class LiveTrader: Trader, ISerializable<LiveTraderState> {
  public string Id => state.id;

  public readonly LiveTraderConfig config;

  public LiveTraderState state;

  private LiveTrader(LiveTraderConfig config, string id): base(
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new LiveBroker(config.broker)
  ) {
    this.config = config;

    (broker as LiveBroker)!.trader = this;

    state = new LiveTraderState(
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

  public override async Task<Trade?> Tick(Candle candle) {
    var trade = await base.Tick(candle);

    if (trade.HasValue) {
      var id = new Visus.Cuid.Cuid2().ToString();

      var order = trade.Value.order;
      if (order != null) {
        var liveTraderOrder = LiveTraderOrder.From(state.id, id, order);

        if (liveTraderOrder.error == null) {
          var client = (broker as LiveBroker)!.client;
          LiveOrderTracker.StartTrack(client, liveTraderOrder);
        }
      }

      var entity = new LiveTraderTrade(
        id: id,
        traderId: Id,
        ts: DateTime.Now,
        signal: trade.Value.signal,
        confidence: trade.Value.confidence,
        orderId: trade.Value.order?.Data?.Id,
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
    state.metrics = JsonSerializer.Serialize(metrics);
    state.states = JsonSerializer.Serialize(new InnerState(firstCandle, lastCandle));

    return trade;
  }

  public static LiveTrader Create(LiveTraderConfig config) {
    var id = TraderIdHelper.GenerateLiveTraderId(config.broker.isTestNet);
    return new LiveTrader(config, id);
  }

  public static async Task<LiveTrader?> Load(string id) {
    var state = await LiveTraderState.Get(id);
    if (state == null) {
      return null;
    }

    var config = state.Value.Config;
    var trader = new LiveTrader(config, id);
    trader.Deserialize(state.Value);

    return trader;
  }

  public LiveTraderState Serialize() {
    return state;
  }

  public void Deserialize(LiveTraderState state) {
    this.state = state;

    strategy.Deserialize(state.strategy_state);
    broker.Deserialize(state.broker_state);
    metrics = JsonSerializer.Deserialize<TraderMetrics>(state.metrics);
    var innerState = JsonSerializer.Deserialize<InnerState>(state.states);
    firstCandle = innerState?.firstCandle;
    lastCandle = innerState?.lastCandle;
  }

  record InnerState(Candle? firstCandle, Candle? lastCandle);
}
