using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class SMMAIndicator : IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private int period;

    private int age;
    private SMAIndicator sma;

    public SMMAIndicator(int period) {
      this.period = period;
      Ready = false;
      Value = 0;

      sma = new SMAIndicator(period);
      age = 0;
    }

    public void Update(Candle candle) {
      age += 1;

      if (age <= period) {
        sma.Update(candle);
        Value = sma.Value;
      } else {
        Value = (Value * (period - 1) + candle.Close) / period;
        Ready = true;
      }
    }

    protected internal record SerializedState(
      bool ready,
      double value,
      int period,
      int age,
      string sma
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializedState(
        Ready,
        Value,
        period,
        age,
        sma.Serialize()
      ));
    }

    public void Deserialize(string data) {
      var parsed = JsonSerializer.Deserialize<SerializedState>(data);
      if (parsed == null) {
        throw new ArgumentException("Failed to deserialize state");
      }

      Ready = parsed.ready;
      Value = parsed.value;
      period = parsed.period;
      age = parsed.age;
      sma = new SMAIndicator(period);
      sma.Deserialize(parsed.sma);
    }
  }

  public static SMMAIndicator SMMA(int period) {
    return new SMMAIndicator(period);
  }
}
