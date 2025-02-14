using System.Dynamic;
using System.Text.Json;

public partial class Indicators {
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
      age += 1;

      highs[age % period] = candle.high;
      lows[age % period] = candle.low;
      closes[age % period] = candle.close;

      var sum = candle.high - candle.low;
      for (var i = 1; i < period; i++) {
        sum += CalcTrueRange(age + i);
      }

      Value = sum / period;

      if (!Ready && age >= period) {
        if (age == period - 1) {
          Ready = true;
        }
      }
    }

    private double CalcTrueRange(int i) {
      var l = lows[i % period];
      var h = highs[i % period];
      var c = closes[(i - 1) % period];
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

    public string Serialize() {
      return JsonSerializer.Serialize(new {
        Ready,
        Value,
        period,
        age,
        highs,
        lows,
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
      highs = parsed.highs;
      lows = parsed.lows;
      closes = parsed.closes;
    }
  }

  public static ATRIndicator ATR(int period) {
    return new ATRIndicator(period);
  }
}
