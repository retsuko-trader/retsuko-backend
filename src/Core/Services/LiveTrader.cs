using System.Text.Json;
using Retsuko.Core.Events;
using Retsuko.Plugins;

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

  public override async Task Preload(ICandleLoader loader) {
    await base.Preload(loader);
    state.strategy_state = strategy.Serialize();
  }

  public override async Task<Trade?> Tick(Candle candle) {
    if (!firstCandle.HasValue) {
      firstCandle = candle;
    }

    var delay = DateTime.UtcNow - candle.ts - candle.interval.ToTimeSpan();
    var delayed = delay > TimeSpan.FromHours(1);
    if (delayed)  {
      MyLogger.Logger.LogWarning(
        "LiveTrader {traderId} tick delayed {delay} for candle {candle}",
        Id,
        delay,
        candle
      );
    }

    var signal = await strategy.Update(candle);
    return await HandleSignal(candle, signal, delayed, delay);
  }

  public async Task<Trade?> HandleSignal(Candle candle, Signal? signal, bool delayed = false, TimeSpan? delay = null, bool force = false) {
    Trade? trade = null;

    if (signal != null) {
      if (!delayed) {
        trade = await broker.HandleAdvice(candle, signal, force);
      } else {
        MyLogger.Logger.LogError(
          "LiveTrader {traderId} got signal {signal} but delayed {delay} for candle {candle}",
          Id,
          signal,
          delay,
          candle
        );

        EventDispatcher.Event(new LiveBrokerOrderDelayedEvent(this, candle, signal));
      }
      if (trade.HasValue) {
        if (trades.Count > 0) {
          var lastTrade = trades[^1];

          if (lastTrade.signal == SignalKind.openLong || lastTrade.signal == SignalKind.openShort) {
            var currBalance = trade.Value.TotalBalance;
            var prevBalance = lastTrade.TotalBalance;
            var profit = (currBalance - prevBalance) / prevBalance;

            lastTrade.profit = profit;
            trades[^1] = lastTrade;
          }
        }

        trades.Add(trade.Value);
      }

      ProcessMetrics(candle, trade);
    }

    lastCandle = candle;

    if (trade.HasValue) {
      var id = new Visus.Cuid.Cuid2().ToString();

      var order = trade.Value.order;
      if (order != null) {
        var liveTraderOrder = LiveTraderOrder.From(state.id, id, order);
        var client = (broker as LiveBroker)!.client;
        LiveOrderTracker.StartTrack(client, trade.Value, liveTraderOrder);

        if (liveTraderOrder.error != null) {
          MyLogger.Logger.LogError("Failed to create order {orderId}: {error}", id, liveTraderOrder.error);
        }
      }
    }

    state.updatedAt = DateTime.Now;
    state.strategy_state = strategy.Serialize();
    state.broker_state = broker.Serialize();

    try {
      state.metrics = JsonSerializer.Serialize(metrics);
    } catch (Exception ex) {
      MyLogger.Logger.LogError(ex, "Failed to serializing metrics: {metrics}", metrics);
      EventDispatcher.Exception(null, ex);
    }

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
