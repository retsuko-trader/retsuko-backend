using System.Dynamic;
using System.Text.Json;
using Retsuko.Core;
using Retsuko.Core.Indicators;

namespace Retsuko.Strategies;

public record struct SuperTrendStrategyConfig(
  int atrPeriod,
  float bandFactor,
  float trailingStop,
  float confidenceMultiplier,
  float confidenceBias
);

public class SuperTrendStrategy: Strategy<SuperTrendStrategyConfig>, IStrategyCreate<SuperTrendStrategy> {
  record struct State(
    double upperBandBasic,
    double lowerBandBasic,
    double upperBand,
    double lowerBand,
    double superTrend
) {}

  private IIndicator atr;
  private TrailingStopLoss stopLoss;
  private State trend;
  private State lastTrend;
  private double lastClose;
  private int age;
  private double confidence;
  private double prevBuyConfidence;

  public static string DefaultConfig => JsonSerializer.Serialize(new SuperTrendStrategyConfig {
    atrPeriod = 7,
    bandFactor = 3,
    trailingStop = 3.5f,
    confidenceMultiplier = 20,
    confidenceBias = 0.1f,
  });

  public static SuperTrendStrategy Create(string config) {
    return new SuperTrendStrategy(JsonSerializer.Deserialize<SuperTrendStrategyConfig>(config));
  }

  public SuperTrendStrategy(SuperTrendStrategyConfig config): base(config) {
    atr = AddIndicator(Indicators.ATR(config.atrPeriod));
    trend = new State(0, 0, 0, 0, 0);
    lastTrend = new State(0, 0, 0, 0, 0);
    stopLoss = new TrailingStopLoss(config.trailingStop);
  }

  public override async Task Preload(IEnumerable<Candle> candles) {
    await base.Preload(candles);
    foreach (var candle in candles) {
      UpdateInner(candle);
    }
  }

  public override async Task<Signal?> Update(Candle candle) {
    await base.Update(candle);

    var ready = UpdateInner(candle);
    if (!ready) {
      return null;
    }

    if (stopLoss.IsTriggered(candle.close)) {
      stopLoss.End();
      prevBuyConfidence = 0;
      return Signal.closeLong;
    }

    if (candle.close > trend.superTrend) {
      stopLoss.Begin(candle.close);
      var conf = Math.Min(1, confidence * Config.confidenceMultiplier);
      if (conf - prevBuyConfidence < Config.confidenceBias) {
        return null;
      }

      prevBuyConfidence = conf;
      return new Signal(SignalKind.@long, conf);
    }

    if (candle.close < trend.superTrend) {
      stopLoss.End();
      prevBuyConfidence = 0;
      return Signal.@short;
    }

    return null;
  }

  private bool UpdateInner(Candle candle) {
    var atr = this.atr.Value;
    age += 1;

    if (age < Config.atrPeriod) {
      return false;
    }

    var close = candle.close;

    trend.upperBandBasic = (candle.high + candle.low) / 2 + atr * Config.bandFactor;
    trend.lowerBandBasic = (candle.high + candle.low) / 2 - atr * Config.bandFactor;

    if (trend.upperBandBasic < lastTrend.upperBand || lastClose > lastTrend.upperBand) {
      trend.upperBand = trend.upperBandBasic;
    } else {
      trend.upperBand = lastTrend.upperBand;
    }

    if (trend.lowerBandBasic > lastTrend.lowerBand || lastClose < lastTrend.lowerBand) {
      trend.lowerBand = trend.lowerBandBasic;
    } else {
      trend.lowerBand = lastTrend.lowerBand;
    }

    if (lastTrend.superTrend == lastTrend.upperBand && close <= trend.upperBand) {
      trend.superTrend = trend.upperBand;
      confidence = (trend.upperBand - close) / close;
    } else if (lastTrend.superTrend == lastTrend.upperBand && close >= trend.upperBand) {
      trend.superTrend = trend.lowerBand;
      confidence = (close - trend.lowerBand) / close;
    } else if (lastTrend.superTrend == lastTrend.lowerBand && close >= trend.lowerBand) {
      trend.superTrend = trend.lowerBand;
      confidence = (close - trend.lowerBand) / close;
    } else if (lastTrend.superTrend == lastTrend.lowerBand && close <= trend.lowerBand) {
      trend.superTrend = trend.upperBand;
      confidence = (trend.upperBand - close) / close;
    } else {
      trend.superTrend = 0;
    }

    lastClose = close;
    lastTrend = trend;
    return true;
  }

  public override string Serialize() {
    return JsonSerializer.Serialize(new {
      Config,
      trend,
      lastTrend,
      lastClose,
      age,
      confidence,
      prevBuyConfidence,
      stopLoss = stopLoss.Serialize()
    });
  }

  public override void Deserialize(string data) {
    dynamic? parsed = JsonSerializer.Deserialize<ExpandoObject>(data);
    if (parsed == null) {
      return;
    }

    Config = parsed.Config;
    trend = parsed.trend;
    lastTrend = parsed.lastTrend;
    lastClose = parsed.lastClose;
    age = parsed.age;
    confidence = parsed.confidence;
    prevBuyConfidence = parsed.prevBuyConfidence;
    stopLoss.Deserialize(parsed.stopLoss);
  }
}
