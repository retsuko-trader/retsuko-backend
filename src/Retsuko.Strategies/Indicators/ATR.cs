using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class ATRIndicator: IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private int period;

    private int age;
    private double[] highs;
    private double[] lows;
    private double[] closes;

    public ATRIndicator(int period) {
      this.period = period;
      Ready = false;

      highs = new double[period];
      lows = new double[period];
      closes = new double[period];
    }

    public void Update(Candle candle) {
      highs.GetByMod(age) = candle.High;
      lows.GetByMod(age) = candle.Low;
      closes.GetByMod(age) = candle.Close;

      var sum = highs.GetByMod(age + 1) - lows.GetByMod(age + 1);
      for (var i = 1; i < period; i++) {
        sum += CalcTrueRange(age + i + 1);
      }

      Value = sum / period;

      if (!Ready && age + 1 >= period) {
        Ready = true;
      }

      age += 1;
    }

    private double CalcTrueRange(int i) {
      var l = lows.GetByMod(i);
      var h = highs.GetByMod(i);
      var c = closes.GetByMod(i - 1);
      var ych = Math.Abs(h - c);
      var ycl = Math.Abs(l - c);
      var v = h - l;
      if (ych > v) {
        v = ych;
      }
      if (ycl > v) {
        v = ycl;
      }

      return v;
    }

    record SerializedState(
      bool Ready,
      double Value,
      int period,
      int age,
      double[] highs,
      double[] lows,
      double[] closes
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializedState(
        Ready,
        Value,
        period,
        age,
        highs,
        lows,
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
      highs = parsed.highs;
      lows = parsed.lows;
      closes = parsed.closes;
    }
  }

  public static ATRIndicator ATR(int period) {
    return new ATRIndicator(period);
  }
}
