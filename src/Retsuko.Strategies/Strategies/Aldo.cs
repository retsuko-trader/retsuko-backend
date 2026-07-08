using System.Text.Json;
using Retsuko.Strategies.Core;
using Retsuko.Strategies.Indicators;
using Retsuko.Strategies.Services;
using Retsuko.Strategies.Utilities;

namespace Retsuko.Strategies.Strategies;

public record struct AldoStrategyConfig(
  float trailingStopLoss,
  int exitBars,
  int rsiLength,
  int macdFast,
  int macdSlow,
  int macdSignal,
  int stochLength,
  int stochSmoothK,
  int stochSmoothD,
  int cciLength,
  int period,
  int overbought,
  int oversold,
  // 0: CCI, 1: RSI, 2: MACD
  int oscillatorType
);

public class AldoStrategy : Strategy<AldoStrategyConfig>, IStrategyCreate<AldoStrategy> {
  private IIndicator oscillator;
  private Candle[] candles;
  private double[] oscs;
  private int age;

  private TrailingStopLoss stopLoss;
  private int exitAge;
  private double lastPriceHigh, lastPriceLow;
  private double prevPriceHigh, prevPriceLow;
  private double lastOscHigh, lastOscLow;
  private double prevOscHigh, prevOscLow;

  public static string Name => "Aldo";
  public static string DefaultConfig => JsonSerializer.Serialize(new AldoStrategyConfig {
    trailingStopLoss = 3.5f,
    exitBars = 7,
    rsiLength = 14,
    macdFast = 12,
    macdSlow = 26,
    macdSignal = 9,
    stochLength = 14,
    stochSmoothK = 3,
    stochSmoothD = 3,
    cciLength = 20,
    period = 5,
    overbought = 70,
    oversold = 30,
    oscillatorType = 0,
  });

  public static AldoStrategy Create(string config) {
    return new AldoStrategy(JsonSerializer.Deserialize<AldoStrategyConfig>(config));
  }

  public AldoStrategy(AldoStrategyConfig config) : base(config) {
    if (config.oscillatorType == 0) {
      oscillator = AddIndicator(Indicator.CCI(config.cciLength));
    } else if (config.oscillatorType == 1) {
      oscillator = AddIndicator(Indicator.RSI(config.rsiLength));
    } else {
      oscillator = AddIndicator(Indicator.MACD(config.macdFast, config.macdSlow, config.macdSignal));
    }

    candles = new Candle[config.period];
    oscs = new double[config.period];
    age = 0;
    stopLoss = new TrailingStopLoss(config.trailingStopLoss);
  }

  public override async Task Preload(Candle candle) {
    await base.Preload(candle);
    UpdateInner(candle);
  }

  public override async Task<Signal?> Update(Candle candle) {
    await base.Update(candle);
    return UpdateInner(candle);
  }

  private Signal? UpdateInner(Candle candle) {
    candles.GetByMod(age) = candle;
    oscs.GetByMod(age) = oscillator.Value;
    age += 1;

    if (age < Config.period) {
      return null;
    }

    var osc = oscillator.Value;

    var priceHigh = candle.High >= candles.MaxBy(x => x.High)!.High;
    var priceLow = candle.Low <= candles.MinBy(x => x.Low)!.Low;
    var oscHigh = osc >= oscs.Max();
    var oscLow = osc <= oscs.Min();

    var lastPriceHigh = priceHigh ? candle.High : this.lastPriceHigh;
    var prevPriceHigh = priceHigh ? lastPriceHigh : this.prevPriceHigh;

    var lastOscHigh = oscHigh ? osc : this.lastOscHigh;
    var prevOscHigh = oscHigh ? lastOscHigh : this.prevOscHigh;

    var lastPriceLow = priceLow ? candle.Low : this.lastPriceLow;
    var prevPriceLow = priceLow ? lastPriceLow : this.prevPriceLow;

    var lastOscLow = oscLow ? osc : this.lastOscLow;
    var prevOscLow = oscLow ? lastOscLow : this.prevOscLow;

    var regularBearish = priceHigh && (candle.High >= prevPriceHigh) && (osc <= prevOscHigh);
    var hiddenBearish = priceHigh && (candle.High <= prevPriceHigh) && (osc <= prevOscHigh);
    var bearishDiv = regularBearish || hiddenBearish;

    var regularBullish = priceLow && (candle.Low <= prevPriceLow) && (osc >= prevOscLow);
    var hiddenBullish = priceLow && (candle.Low >= prevPriceLow) && (osc <= prevOscLow);
    var bullishDiv = regularBullish || hiddenBullish;

    this.lastPriceHigh = lastPriceHigh;
    this.prevPriceHigh = prevPriceHigh;
    this.lastOscHigh = lastOscHigh;
    this.prevOscHigh = prevOscHigh;
    this.lastPriceLow = lastPriceLow;
    this.prevPriceLow = prevPriceLow;
    this.lastOscLow = lastOscLow;
    this.prevOscLow = prevOscLow;

    if (age == exitAge) {
      stopLoss.End();
      return Signal.closeLong;
    }

    if (stopLoss.IsTriggered(candle.Close)) {
      stopLoss.End();
      return Signal.closeLong;
    }

    if (bullishDiv) {
      stopLoss.Begin(candle.Close);
      exitAge = age + Config.exitBars;
      return Signal.openLong;
    }

    return null;
  }

  public override StrategyConsistencyResult CheckConsistency() {
    return new StrategyConsistencyResult(
      true,
      []
    );
  }

  public override async Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle) {
    await base.Debug(candle);

    if (age < Config.period) {
      return [];
    }

    var osc = oscillator.Value;
    var highPrice = candles.MaxBy(x => x.High)!.High;
    var lowPrice = candles.MinBy(x => x.Low)!.Low;
    var highOsc = oscs.Max();
    var lowOsc = oscs.Min();

    var priceHigh = candle.High >= highPrice;
    var priceLow = candle.Low <= lowPrice;
    var oscHigh = osc >= highOsc;
    var oscLow = osc <= lowOsc;

    return [
        new DebugIndicatorInput("oscillator", 0, (float)oscillator.Value),
      new DebugIndicatorInput("lastPriceHigh", 1, (float)lastPriceHigh),
      new DebugIndicatorInput("prevPriceHigh", 1, (float)prevPriceHigh),
      new DebugIndicatorInput("lastPriceLow", 1, (float)lastPriceLow),
      new DebugIndicatorInput("prevPriceLow", 1, (float)prevPriceLow),
      new DebugIndicatorInput("lastOscHigh", 2, (float)lastOscHigh),
      new DebugIndicatorInput("prevOscHigh", 2, (float)prevOscHigh),
      new DebugIndicatorInput("lastOscLow", 2, (float)lastOscLow),
      new DebugIndicatorInput("prevOscLow", 2, (float)prevOscLow),
      new DebugIndicatorInput("priceHigh", 3, priceHigh ? 1 : 0),
      new DebugIndicatorInput("priceLow", 3, priceLow ? 1 : 0),
      new DebugIndicatorInput("oscHigh", 3, oscHigh ? 1 : 0),
      new DebugIndicatorInput("oscLow", 3, oscLow ? 1 : 0),
      new DebugIndicatorInput("highPrice", 4, (float)highPrice),
      new DebugIndicatorInput("lowPrice", 4, (float)lowPrice),
      new DebugIndicatorInput("highOsc", 5, (float)highOsc),
      new DebugIndicatorInput("lowOsc", 5, (float)lowOsc)
    ];
  }

  record SerializedState(
    AldoStrategyConfig config,
    string oscillator,
    Candle[] candles,
    double[] oscs,
    double lastPriceHigh,
    double prevPriceHigh,
    double lastPriceLow,
    double prevPriceLow,
    double lastOscHigh,
    double prevOscHigh,
    double lastOscLow,
    double prevOscLow,
    int age,
    string stopLoss
  );

  public override string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      Config,
      oscillator.Serialize(),
      candles,
      oscs,
      lastPriceHigh,
      prevPriceHigh,
      lastPriceLow,
      prevPriceLow,
      lastOscHigh,
      prevOscHigh,
      lastOscLow,
      prevOscLow,
      age,
      stopLoss.Serialize()
    ));
  }

  public override void Deserialize(string data) {
    var parsed = JsonSerializer.Deserialize<SerializedState>(data);
    if (parsed == null) {
      throw new ArgumentException("Failed to deserialize state");
    }

    Config = parsed.config;
    oscillator.Deserialize(parsed.oscillator);
    candles = parsed.candles;
    oscs = parsed.oscs;
    lastPriceHigh = parsed.lastPriceHigh;
    prevPriceHigh = parsed.prevPriceHigh;
    lastPriceLow = parsed.lastPriceLow;
    prevPriceLow = parsed.prevPriceLow;
    lastOscHigh = parsed.lastOscHigh;
    prevOscHigh = parsed.prevOscHigh;
    lastOscLow = parsed.lastOscLow;
    prevOscLow = parsed.prevOscLow;
    age = parsed.age;
    stopLoss.Deserialize(parsed.stopLoss);
  }
}
