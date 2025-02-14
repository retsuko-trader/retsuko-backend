using System.Dynamic;
using System.Text.Json;

public record struct SuperTrendStrategyConfig {
  public int AtrPeriod { get; init; }
  public float BandFactor { get; init; }
  public float TrailingStop { get; init; }
  public float ConfidenceMultiplier { get; init; }
  public float ConfidenceBias { get; init; }
}

public class SuperTrendStrategy: Strategy<SuperTrendStrategyConfig> {
  record struct State(
    double upperBandBasic,
    double lowerBandBasic,
    double upperBand,
    double lowerBand,
    double superTrend
) {}

  private SuperTrendStrategyConfig config;

  private IIndicator atr;
  private State trend;
  private State lastTrend;
  private double lastClose;
  private int age;
  private double confidence;
  private double prevBuyConfidence;

  public SuperTrendStrategy(SuperTrendStrategyConfig config): base(config) {
    this.config = config;

    atr = AddIndicator(Indicators.ATR(config.AtrPeriod));
    trend = new State(0, 0, 0, 0, 0);
    lastTrend = new State(0, 0, 0, 0, 0);
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

    if (candle.close > trend.superTrend) {
      var conf = Math.Min(1, confidence * config.ConfidenceMultiplier);
      if (conf - prevBuyConfidence < config.ConfidenceBias) {
        return null;
      }

      prevBuyConfidence = conf;
      return new Signal(SignalKind.@long, conf);
    }

    if (candle.close < trend.superTrend) {
      prevBuyConfidence = 0;
      return Signal.@short;
    }

    return null;
  }

  private bool UpdateInner(Candle candle) {
    var atr = this.atr.Value;
    age += 1;

    if (age < config.AtrPeriod) {
      return false;
    }

    var close = candle.close;

    trend.upperBandBasic = (candle.high + candle.low) / 2 + atr * config.BandFactor;
    trend.lowerBandBasic = (candle.high + candle.low) / 2 - atr * config.BandFactor;

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
      confidence = (trend.upperBand - close) / (close * atr);
    } else if (lastTrend.superTrend == lastTrend.upperBand && close >= trend.upperBand) {
      trend.superTrend = trend.lowerBand;
      confidence = (close - trend.lowerBand) / (close * atr);
    } else if (lastTrend.superTrend == lastTrend.lowerBand && close >= trend.lowerBand) {
      trend.superTrend = trend.lowerBand;
      confidence = (close - trend.lowerBand) / (close * atr);
    } else if (lastTrend.superTrend == lastTrend.lowerBand && close <= trend.lowerBand) {
      trend.superTrend = trend.upperBand;
      confidence = (trend.upperBand - close) / (close * atr);
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
  }
}
