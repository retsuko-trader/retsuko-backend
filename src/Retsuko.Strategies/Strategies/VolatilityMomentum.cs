using System.Text.Json;
using Retsuko.Strategies.Core;
using Retsuko.Strategies.Indicators;
using Retsuko.Strategies.Services;

namespace Retsuko.Strategies.Strategies;

public record struct VolatilityMomentumStrategyConfig(
  int momentumLookback,
  int trendPeriod,
  int breakoutLookback,
  int exitLookback,
  int atrPeriod,
  double minMomentum,
  double momentumScale,
  double targetAtrFraction,
  double minConfidence,
  double maxConfidence,
  double confidenceStep,
  double stopAtrMultiplier
);

public class VolatilityMomentumStrategy: Strategy<VolatilityMomentumStrategyConfig>, IStrategyCreate<VolatilityMomentumStrategy> {
  private Candle[] candles;
  private int age;
  private int candlesLength;

  private IIndicator trend;
  private IIndicator atr;

  private bool inLong;
  private double peakClose;
  private double lastConfidence;
  private double momentum;
  private double highestClose;
  private double lowestClose;
  private double normalizedAtr;
  private double confidence;

  public static string Name => "VolatilityMomentum";
  public static string DefaultConfig => JsonSerializer.Serialize(new VolatilityMomentumStrategyConfig {
    momentumLookback = 195,
    trendPeriod = 195,
    breakoutLookback = 90,
    exitLookback = 45,
    atrPeriod = 42,
    minMomentum = 0.03,
    momentumScale = 0.35,
    targetAtrFraction = 0.05,
    minConfidence = 0.35,
    maxConfidence = 1.0,
    confidenceStep = 0.1,
    stopAtrMultiplier = 3.0,
  });

  public static VolatilityMomentumStrategy Create(string config) {
    return new VolatilityMomentumStrategy(JsonSerializer.Deserialize<VolatilityMomentumStrategyConfig>(config));
  }

  public VolatilityMomentumStrategy(VolatilityMomentumStrategyConfig config): base(config) {
    candlesLength = Math.Max(
      Math.Max(config.momentumLookback + 1, config.trendPeriod + 1),
      Math.Max(config.breakoutLookback + 1, Math.Max(config.exitLookback + 1, config.atrPeriod + 1))
    );
    candles = new Candle[candlesLength];
    trend = AddIndicator(Indicator.SMA(config.trendPeriod));
    atr = AddIndicator(Indicator.ATR(config.atrPeriod));
  }

  public override async Task Preload(Candle candle) {
    await base.Preload(candle);
    UpdateInner(candle, false);
  }

  public override async Task<Signal?> Update(Candle candle) {
    await base.Update(candle);
    return UpdateInner(candle, true);
  }

  private Signal? UpdateInner(Candle candle, bool shouldSignal) {
    var ready = age >= candlesLength && trend.Ready && atr.Ready && candle.Close > 0;
    Signal? signal = null;

    if (ready) {
      var momentumRef = candles.GetByMod(age - Config.momentumLookback);
      momentum = candle.Close / momentumRef.Close - 1;

      (highestClose, _) = CalculateChannel(Config.breakoutLookback);
      (_, lowestClose) = CalculateChannel(Config.exitLookback);

      normalizedAtr = atr.Value / candle.Close;
      confidence = CalculateConfidence();

      if (inLong) {
        peakClose = Math.Max(peakClose, candle.Close);
      }

      if (shouldSignal) {
        var bullish = candle.Close > trend.Value && momentum > Config.minMomentum;
        var breakout = candle.Close > highestClose;
        var stopTriggered = inLong && candle.Close < peakClose - atr.Value * Config.stopAtrMultiplier;
        var exitTriggered = inLong && (
          candle.Close < trend.Value ||
          momentum < -Config.minMomentum ||
          candle.Close < lowestClose ||
          stopTriggered
        );

        if (exitTriggered) {
          inLong = false;
          peakClose = 0;
          lastConfidence = 0;
          signal = Signal.closeLong;
        } else if (!inLong && bullish && breakout) {
          inLong = true;
          peakClose = candle.Close;
          lastConfidence = confidence;
          signal = new Signal(SignalKind.openLong, confidence);
        } else if (inLong && confidence > lastConfidence + Config.confidenceStep && bullish) {
          lastConfidence = confidence;
          signal = new Signal(SignalKind.openLong, confidence);
        }
      }
    }

    candles.GetByMod(age) = candle;
    age += 1;

    return signal;
  }

  private double CalculateConfidence() {
    var momentumWeight = Math.Clamp(momentum / Config.momentumScale, Config.minConfidence, Config.maxConfidence);
    var volatilityWeight = normalizedAtr <= 0
      ? Config.maxConfidence
      : Math.Clamp(Config.targetAtrFraction / normalizedAtr, Config.minConfidence, Config.maxConfidence);

    return Math.Clamp(momentumWeight * volatilityWeight, Config.minConfidence, Config.maxConfidence);
  }

  private (double high, double low) CalculateChannel(int count) {
    var high = double.MinValue;
    var low = double.MaxValue;

    for (var i = 1; i <= count; i++) {
      var candle = candles.GetByMod(age - i);
      high = Math.Max(high, candle.Close);
      low = Math.Min(low, candle.Close);
    }

    return (high, low);
  }

  public override StrategyConsistencyResult CheckConsistency() {
    var errors = new List<string>();

    try {
      ConsistencyHelper.CheckCandlesTimestamps(errors, candles, age);
    } catch (Exception ex) {
      errors.Add($"exception during consistency check: {ex}");
    }

    return new StrategyConsistencyResult(
      IsSuccess: errors.Count == 0,
      Errors: errors
    );
  }

  static readonly string MOMENTUM_NAME = string.Intern("momentum");
  static readonly string TREND_NAME = string.Intern("trend");
  static readonly string ATR_NAME = string.Intern("atr");
  static readonly string CONFIDENCE_NAME = string.Intern("confidence");

  public override async Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle) {
    await ValueTask.CompletedTask;

    return [
      new(MOMENTUM_NAME, 0, (float)momentum),
      new(TREND_NAME, 1, (float)trend.Value),
      new(ATR_NAME, 2, (float)atr.Value),
      new(CONFIDENCE_NAME, 3, (float)confidence),
    ];
  }

  public override object? Dump() {
    var candles = new Candle[candlesLength];
    for (var i = 0; i < candlesLength; i++) {
      candles[i] = this.candles.GetByMod(i + age);
    }

    return new {
      candles,
      age,
      candlesLength,
      inLong,
      peakClose,
      lastConfidence,
      momentum,
      highestClose,
      lowestClose,
      normalizedAtr,
      confidence,
      trend = trend.Serialize(),
      atr = atr.Serialize(),
    };
  }

  record SerializedState(
    VolatilityMomentumStrategyConfig Config,
    Candle[] candles,
    int age,
    int candlesLength,
    bool inLong,
    double peakClose,
    double lastConfidence,
    double momentum,
    double highestClose,
    double lowestClose,
    double normalizedAtr,
    double confidence,
    string trend,
    string atr
  );

  public override string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      Config,
      candles,
      age,
      candlesLength,
      inLong,
      peakClose,
      lastConfidence,
      momentum,
      highestClose,
      lowestClose,
      normalizedAtr,
      confidence,
      trend.Serialize(),
      atr.Serialize()
    ));
  }

  public override void Deserialize(string data) {
    var parsed = JsonSerializer.Deserialize<SerializedState>(data);
    if (parsed == null) {
      return;
    }

    Config = parsed.Config;
    candles = parsed.candles;
    age = parsed.age;
    candlesLength = parsed.candlesLength;
    inLong = parsed.inLong;
    peakClose = parsed.peakClose;
    lastConfidence = parsed.lastConfidence;
    momentum = parsed.momentum;
    highestClose = parsed.highestClose;
    lowestClose = parsed.lowestClose;
    normalizedAtr = parsed.normalizedAtr;
    confidence = parsed.confidence;
    trend.Deserialize(parsed.trend);
    atr.Deserialize(parsed.atr);
  }
}
