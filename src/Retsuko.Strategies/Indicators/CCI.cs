using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class CCIIndicator : IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private int period;
    private double constant;

    private int age;
    private double[] typicals;

    public CCIIndicator(int period, double constant = 0.015) {
      this.period = period;
      this.constant = constant;
      Ready = false;
      Value = 0;
      typicals = new double[period];
      age = 0;
    }

    public void Update(Candle candle) {
      var typical = (candle.High + candle.Low + candle.Close) / 3.0;
      typicals.GetByMod(age) = typical;

      if (age < period - 1) {
        age += 1;
        return;
      }

      Ready = true;

      var avgTp = typicals.Average();
      var sum = 0.0;
      foreach (var tp in typicals) {
        sum += Math.Abs(tp - avgTp);
      }

      var mean = sum / period;
      Value = (typical - avgTp) / (constant * mean);
      age += 1;
    }

    protected internal record SerializedState(
      bool ready,
      double value,
      int period,
      double constant,
      int age,
      double[] typicals
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializedState(
        Ready,
        Value,
        period,
        constant,
        age,
        typicals
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
      constant = parsed.constant;
      age = parsed.age;
      typicals = parsed.typicals;
    }
  }

  public static CCIIndicator CCI(int period, double constant = 0.015) {
    return new CCIIndicator(period, constant);
  }
}
