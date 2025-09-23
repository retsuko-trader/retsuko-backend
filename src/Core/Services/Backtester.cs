
namespace Retsuko.Core;

public class Backtester: Trader<StrategyLazy> {
  private readonly Dictionary<(string, int), List<DebugIndicator>> debugIndicators = [];

  private readonly BacktestConfig config;

  public Backtester(BacktestConfig config): base(
    StrategyLazy.Create(config.strategy.name, config.strategy.config)!,
    new PaperBroker(config.broker)
  ) {
    this.config = config;
  }

  public override async Task<Trade?> Tick(Candle candle) {
    if (!firstCandle.HasValue) {
      firstCandle = candle;
    }

    await strategy.Update(candle);
    return null;

    // var debugs = await strategy.Debug(candle);
    // foreach (var debug in debugs) {
    //   var key = (debug.name, debug.index);
    //   if (!debugIndicators.TryGetValue(key, out var stack)) {
    //     stack = [];
    //     debugIndicators[key] = stack;
    //   }

    //   stack.Add(new DebugIndicator(
    //     ts: new DateTimeOffset(candle.ts).ToUnixTimeMilliseconds(),
    //     value: debug.value
    //   ));
    //   debugIndicators[key] = stack;
    // }
  }

  public async Task TickBulk(IEnumerable<Candle> candles) {
    if (!firstCandle.HasValue) {
      firstCandle = candles.First();
    }

    await strategy.UpdateBulk(candles);
  }

  public async Task ProcessSignals() {
    await strategy.FinishInputs();
    var result = await strategy.GetUpdateResult();
    while (result.HasValue) {
      await TickManual(result.Value.candle, result.Value.signal);
      result = await strategy.GetUpdateResult();
    }
  }


  public override async Task FinalizeMetrics() {
    await base.FinalizeMetrics();
  }

  public async Task<TraderReport> GetReport() {
    var dts = await strategy.GetDebugIndicators();

    return new TraderReport(
      config,
      trades,
      metrics,
      dts
    );
  }
}
