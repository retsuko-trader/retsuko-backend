using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class MACDIndicator : IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private int shortPeriod;
    private int longPeriod;
    private int signalPeriod;

    private EMAIndicator shortEma;
    private EMAIndicator longEma;
    private EMAIndicator signalEma;

    public MACDIndicator(int shortPeriod, int longPeriod, int signalPeriod) {
      this.shortPeriod = shortPeriod;
      this.longPeriod = longPeriod;
      this.signalPeriod = signalPeriod;

      Ready = false;
      Value = 0;

      shortEma = new EMAIndicator(shortPeriod);
      longEma = new EMAIndicator(longPeriod);
      signalEma = new EMAIndicator(signalPeriod);
    }

    public void Update(Candle candle) {
      shortEma.Update(candle);
      longEma.Update(candle);

      var diff = shortEma.Value - longEma.Value;
      signalEma.Update(new Candle { Close = diff });
      Value = diff - signalEma.Value;
      Ready = true;
    }

    protected internal record SerializedState(
      bool ready,
      double value,
      int shortPeriod,
      int longPeriod,
      int signalPeriod,
      string shortEma,
      string longEma,
      string signalEma
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializedState(
        Ready,
        Value,
        shortPeriod,
        longPeriod,
        signalPeriod,
        shortEma.Serialize(),
        longEma.Serialize(),
        signalEma.Serialize()
      ));
    }

    public void Deserialize(string data) {
      var parsed = JsonSerializer.Deserialize<SerializedState>(data);
      if (parsed == null) {
        throw new ArgumentException("Failed to deserialize state");
      }

      Ready = parsed.ready;
      Value = parsed.value;
      shortPeriod = parsed.shortPeriod;
      longPeriod = parsed.longPeriod;
      signalPeriod = parsed.signalPeriod;

      shortEma = new EMAIndicator(shortPeriod);
      shortEma.Deserialize(parsed.shortEma);

      longEma = new EMAIndicator(longPeriod);
      longEma.Deserialize(parsed.longEma);
      signalEma = new EMAIndicator(signalPeriod);
      signalEma.Deserialize(parsed.signalEma);
    }
  }

  public static MACDIndicator MACD(int shortPeriod, int longPeriod, int signalPeriod) {
    return new MACDIndicator(shortPeriod, longPeriod, signalPeriod);
  }
}
