using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class SMAIndicator: IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private int period;

    private int age;
    private double sum;
    private double[] closes;

    public SMAIndicator(int period) {
      this.period = period;
      Ready = false;

      closes = new double[period];
    }

    public void Update(Candle candle) {
      var tail = closes.GetByMod(age);
      closes.GetByMod(age) = candle.Close;

      sum += candle.Close - tail;
      Value = sum / period;

      if (!Ready && age + 1 >= period) {
        Ready = true;
      }

      age += 1;
    }

    record SerializedState(
      bool Ready,
      double Value,
      int period,
      int age,
      double sum,
      double[] closes
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializedState(
        Ready,
        Value,
        period,
        age,
        sum,
        closes
      ));
    }

    public void Deserialize(string data) {
      var parsed = JsonSerializer.Deserialize<SerializedState>(data);
      if (parsed == null) {
        return;
      }

      Ready = parsed.Ready;
      Value = parsed.Value;
      period = parsed.period;
      age = parsed.age;
      sum = parsed.sum;
      closes = parsed.closes;
    }
  }

  public static SMAIndicator SMA(int period) {
    return new SMAIndicator(period);
  }
}
