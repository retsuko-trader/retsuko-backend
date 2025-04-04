
namespace Retsuko.Core;

public class Backtester: Trader {
  private readonly Dictionary<(string, int), List<DebugIndicator>> debugIndicators = [];

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
      var key = (debug.name, debug.index);
      if (!debugIndicators.TryGetValue(key, out var stack)) {
        stack = [];
        debugIndicators[key] = stack;
      }

      stack.Add(new DebugIndicator(
        ts: (int)candle.ts.ToUnixTimestamp(),
        value: debug.value
      ));
      debugIndicators[key] = stack;
    }

    return trade;
  }

  public TraderReport GetReport() {
    var dts = new List<ExtDebugIndicator>();
    foreach (var (key, stack) in debugIndicators) {
      dts.Add(new ExtDebugIndicator(
        name: key.Item1,
        index: key.Item2,
        values: stack.ToArray()
      ));
    }

    return new TraderReport(
      config,
      trades,
      metrics,
      dts
    );
  }
}
