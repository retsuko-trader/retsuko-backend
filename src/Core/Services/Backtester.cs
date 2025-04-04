
namespace Retsuko.Core;

public class Backtester: Trader {
  private readonly Dictionary<string, List<DebugIndicator>> debugIndicators = [];

  private readonly BacktestConfig config;

  public Backtester(BacktestConfig config): base(
    StrategyLoader.CreateStrategy(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;
  }

  public override async Task<Trade?> Tick(Candle candle) {
    var trade = await  base.Tick(candle);

    var debugs = await strategy.Debug(candle);
    foreach (var debug in debugs) {
      if (!debugIndicators.TryGetValue(debug.name, out var stack)) {
        stack = [];
        debugIndicators[debug.name] = stack;
      }

      stack.Add(new DebugIndicator(
        ts: (int)candle.ts.ToUnixTimestamp(),
        index: debug.index,
        value: debug.value
      ));
      debugIndicators[debug.name] = stack;
    }

    return trade;
  }

  public TraderReport GetReport() {
    return new TraderReport(
      config,
      trades.ToArray(),
      metrics,
      debugIndicators
    );
  }
}
