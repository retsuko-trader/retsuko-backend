using System.Text.Json;
using Retsuko.Core;

namespace Retsuko.Strategies;

public record struct SuperTrendTurtleStrategyConfig(
  int atrPeriod,
  float bandFactor,
  float trailingStop,
  float confidenceMultiplier,
  float confidenceBias,

  int enterFast,
  int exitFast,
  int enterSlow,
  int exitSlow,
  int bullPeriod,
  bool rememberSignal
);

public class SuperTrendTurtleStrategy: Strategy<SuperTrendTurtleStrategyConfig>, IStrategyCreate<SuperTrendTurtleStrategy> {
  private SuperTrendStrategy superTrend;
  private TurtleStrategy turtle;

  private Signal? superTrendSignal;
  private Signal? turtleSignal;

  public static string Name => "SuperTrendTurtle";
  public static string DefaultConfig => JsonSerializer.Serialize(new SuperTrendTurtleStrategyConfig {
    atrPeriod = 7,
    bandFactor = 3,
    trailingStop = 3.5f,
    confidenceMultiplier = 20,
    confidenceBias = 0.1f,
    enterFast = 20,
    exitFast = 10,
    enterSlow = 55,
    exitSlow = 20,
    bullPeriod = 50,
    rememberSignal = false,
  });

  public static SuperTrendTurtleStrategy Create(string config) {
    return new SuperTrendTurtleStrategy(JsonSerializer.Deserialize<SuperTrendTurtleStrategyConfig>(config));
  }

  public SuperTrendTurtleStrategy(SuperTrendTurtleStrategyConfig config): base(config) {
    superTrend = new SuperTrendStrategy(new SuperTrendStrategyConfig {
      atrPeriod = config.atrPeriod,
      bandFactor = config.bandFactor,
      trailingStop = config.trailingStop,
      confidenceMultiplier = config.confidenceMultiplier,
      confidenceBias = config.confidenceBias,
    });
    turtle = new TurtleStrategy(new TurtleStrategyConfig {
      enterFast = config.enterFast,
      exitFast = config.exitFast,
      enterSlow = config.enterSlow,
      exitSlow = config.exitSlow,
      bullPeriod = config.bullPeriod,
    });
  }

  public override async Task<Signal?> Update(Candle candle) {
    await base.Update(candle);

    var newSuperTrendSignal = await superTrend.Update(candle);
    var newTurtleSignal = await turtle.Update(candle);

    if (Config.rememberSignal) {
      superTrendSignal = newSuperTrendSignal;
      turtleSignal = newTurtleSignal;
    } else {
      if (newSuperTrendSignal != null) {
        superTrendSignal = newSuperTrendSignal;
      }
      if (newTurtleSignal != null) {
        turtleSignal = newTurtleSignal;
      }
    }

    if (superTrendSignal == null || turtleSignal == null) {
      return null;
    }

    if (superTrendSignal.kind == SignalKind.openLong && turtleSignal.kind == SignalKind.openLong) {
      return superTrendSignal;
    }

    var superTrendShort = superTrendSignal.kind == SignalKind.openShort || superTrendSignal.kind == SignalKind.closeLong;
    var turtleShort = turtleSignal.kind == SignalKind.openShort || turtleSignal.kind == SignalKind.closeLong;

    if (superTrendShort && turtleShort) {
      return superTrendSignal;
    }

    return null;
  }

  public override async Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle) {
    return await superTrend.Debug(candle);
  }

  record SerializedState(
    string superTrend,
    string turtle,
    Signal? superTrendSignal,
    Signal? turtleSignal
  );

  public override string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      superTrend: superTrend.Serialize(),
      turtle: turtle.Serialize(),
      superTrendSignal,
      turtleSignal
    ));
  }

  public override void Deserialize(string data) {
    var parsed = JsonSerializer.Deserialize<SerializedState>(data);
    if (parsed == null) {
      return;
    }

    superTrend.Deserialize(parsed.superTrend);
    turtle.Deserialize(parsed.turtle);
    superTrendSignal = parsed.superTrendSignal;
    turtleSignal = parsed.turtleSignal;
  }
}
