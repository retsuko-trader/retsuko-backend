using System.Dynamic;
using System.Text.Json;

namespace Retsuko.Core.Indicators;

public partial class Indicators {
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
      closes.GetByMod(age) = candle.close;

      sum += candle.close - tail;
      Value = sum / period;

      if (!Ready && age + 1 >= period) {
        Ready = true;
      }

      age += 1;
    }

    public string Serialize() {
      return JsonSerializer.Serialize(new {
        Ready,
        Value,
        period,
        age,
        sum,
        closes,
      });
    }

    public void Deserialize(string data) {
      dynamic? parsed = JsonSerializer.Deserialize<ExpandoObject>(data);
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
