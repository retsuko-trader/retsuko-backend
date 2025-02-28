using System.Dynamic;
using System.Text.Json;

public record struct TurtleStrategyConfig(
  int enterFast,
  int exitFast,
  int enterSlow,
  int exitSlow,
  int bullPeriod
);

public class TurtleStrategy: Strategy<TurtleStrategyConfig>, IStrategyCreate<TurtleStrategy> {
  enum Result {
    OPEN_FLONG,
    OPEN_FSHORT,
    CLOSE_FAST,
    OPEN_SLONG,
    OPEN_SSHORT,
    CLOSE_SLOW
  }

  private Candle[] candles;
  private int age;
  private int candlesLength;
  private IIndicator sma;

  public static string DefaultConfig => JsonSerializer.Serialize(new TurtleStrategyConfig {
    enterFast = 20,
    exitFast = 10,
    enterSlow = 55,
    exitSlow = 20,
    bullPeriod = 50,
  });

  public static TurtleStrategy Create(string config) {
    return new TurtleStrategy(JsonSerializer.Deserialize<TurtleStrategyConfig>(config));
  }

  public TurtleStrategy(TurtleStrategyConfig config): base(config) {
    candlesLength = Math.Max(
      Math.Max(config.enterFast, config.exitFast),
      Math.Max(config.enterSlow, config.exitSlow)
    );
    candles = new Candle[candlesLength];
    sma = AddIndicator(Indicators.SMA(config.bullPeriod));
  }

  public override async Task<Signal?> Update(Candle candle) {
    await base.Update(candle);

    var status = UpdateInner(candle);

    if (!status.HasValue) {
      return null;
    }

    if (!sma.Ready) {
      return null;
    }

    if (candle.close < sma.Value) {
      return Signal.closeLong;
    }

    if (status == Result.OPEN_FLONG || status == Result.OPEN_SLONG) {
      return Signal.@long;
    }
    if (status == Result.CLOSE_FAST || status == Result.CLOSE_SLOW) {
      return Signal.@short;
    }

    return null;
  }

  private Result? UpdateInner(Candle candle) {
    candles.GetByMod(age) = candle;
    age += 1;

    var price = candle.close;
    var status = (Result?)null;

    if (age > Config.enterFast) {
      var (high, _) = calculateBreakout(Config.enterFast);
      if (price == high) {
        status = Result.OPEN_FLONG;
      }
    }
    if (age > Config.exitFast) {
      var (_, low) = calculateBreakout(Config.exitFast);
      if (price == low) {
        status = Result.CLOSE_FAST;
      }
    }
    if (age > Config.enterSlow) {
      var (high, _) = calculateBreakout(Config.enterSlow);
      if (price == high) {
        status = Result.OPEN_SLONG;
      }
    }
    if (age > Config.exitSlow) {
      var (_, low) = calculateBreakout(Config.exitSlow);
      if (price == low) {
        status = Result.CLOSE_SLOW;
      }
    }

    return status;
  }

  private (double high, double low) calculateBreakout(int count) {
    var candle = candles.GetByMod(age - count);
    var high = candle.close;
    var low = candle.close;

    for (var i = 0; i < count; i++) {
      candle = candles.GetByMod(age - i);
      high = Math.Max(high, candle.close);
      low = Math.Min(low, candle.close);
    }

    return (high, low);
  }

  public override string Serialize() {
    return JsonSerializer.Serialize(new {
      Config,
      candles,
      age,
      candlesLength
    });
  }

  public override void Deserialize(string data) {
    dynamic? parsed = JsonSerializer.Deserialize<ExpandoObject>(data);
    if (parsed == null) {
      return;
    }

    Config = parsed.Config;
    candles = parsed.candles;
    age = parsed.age;
    candlesLength = parsed.candlesLength;
  }
}
