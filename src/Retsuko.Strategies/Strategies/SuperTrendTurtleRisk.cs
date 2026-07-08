using System.Text.Json;
using Retsuko.Strategies.Core;
using Retsuko.Strategies.Services;
using Retsuko.Strategies.Utilities;

namespace Retsuko.Strategies.Strategies;

public record struct SuperTrendTurtleRiskStrategyConfig(
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
  bool rememberSignal,

  int maxContinuousLosses,
  float maxLossConfidenceMultiplier
);

public class SuperTrendTurtleRiskStrategy: Strategy<SuperTrendTurtleRiskStrategyConfig>, IStrategyCreate<SuperTrendTurtleRiskStrategy> {
  private SuperTrendStrategy superTrend;
  private TurtleStrategy turtle;

  private Signal? superTrendSignal;
  private Signal? turtleSignal;

  private LossConfidenceMultiplier confidenceMultiplier;

  public static string Name => "SuperTrendTurtleRisk";
  public static string DefaultConfig => JsonSerializer.Serialize(new SuperTrendTurtleRiskStrategyConfig {
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
    maxContinuousLosses = 5,
    maxLossConfidenceMultiplier = 0.15f,
  });

  public static SuperTrendTurtleRiskStrategy Create(string config) {
    return new SuperTrendTurtleRiskStrategy(JsonSerializer.Deserialize<SuperTrendTurtleRiskStrategyConfig>(config));
  }

  public SuperTrendTurtleRiskStrategy(SuperTrendTurtleRiskStrategyConfig config): base(config) {
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

    confidenceMultiplier = new LossConfidenceMultiplier(config.maxContinuousLosses, config.maxLossConfidenceMultiplier);
  }

  public override async Task Preload(Candle candle) {
    await base.Preload(candle);
    await superTrend.Preload(candle);
    await turtle.Preload(candle);
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
      confidenceMultiplier.Buy(candle.Close);
      return superTrendSignal.WithMultiplyConfidence(confidenceMultiplier.GetMultiplier());
    }

    var superTrendShort = superTrendSignal.kind == SignalKind.openShort || superTrendSignal.kind == SignalKind.closeLong;
    var turtleShort = turtleSignal.kind == SignalKind.openShort || turtleSignal.kind == SignalKind.closeLong;

    if (superTrendShort && turtleShort) {
      confidenceMultiplier.Sell(candle.Close);
      return superTrendSignal;
    }

    return null;
  }

  public override StrategyConsistencyResult CheckConsistency() {
    var superTrendConsistency = superTrend.CheckConsistency();
    var turtleConsistency = turtle.CheckConsistency();

    var errors = new List<string>();
    errors.AddRange(superTrendConsistency.Errors);
    errors.AddRange(turtleConsistency.Errors);

    return new StrategyConsistencyResult(
      IsSuccess: errors.Count == 0,
      Errors: errors
    );
  }

  public override async Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle) {
    return await superTrend.Debug(candle);
  }

  public override object? Dump() {
    return new {
      superTrend = superTrend.Dump(),
      turtle = turtle.Dump(),
      superTrendSignal,
      turtleSignal,
      confidenceMultiplier = confidenceMultiplier.Serialize(),
    };
  }

  record SerializedState(
    string superTrend,
    string turtle,
    Signal? superTrendSignal,
    Signal? turtleSignal,
    string confidenceMultiplier
  );

  public override string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      superTrend: superTrend.Serialize(),
      turtle: turtle.Serialize(),
      superTrendSignal,
      turtleSignal,
      confidenceMultiplier.Serialize()
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
    confidenceMultiplier.Deserialize(parsed.confidenceMultiplier);
  }
}
