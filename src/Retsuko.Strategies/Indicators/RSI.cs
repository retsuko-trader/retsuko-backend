using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class RSIIndicator : IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private int period;

    private double u;
    private double d;
    private double rs;
    private int age;
    private double lastPrice = -1;

    private SMMAIndicator avgU;
    private SMMAIndicator avgD;

    public RSIIndicator(int period) {
      this.period = period;
      Ready = false;
      Value = 0;

      u = 0;
      d = 0;
      rs = 0;
      age = 0;
      lastPrice = -1;

      avgU = new SMMAIndicator(period);
      avgD = new SMMAIndicator(period);
    }

    public void Update(Candle candle) {
      var price = candle.Close;

      if (lastPrice == -1) {
        lastPrice = price;
        age += 1;
        return;
      }

      if (!Ready) {
        if (avgU.Ready && avgD.Ready) {
          Ready = true;
        }
      }

      if (price > lastPrice) {
        u = price - lastPrice;
        d = 0;
      } else if (price < lastPrice) {
        u = 0;
        d = lastPrice - price;
      } else {
        u = 0;
        d = 0;
      }

      var uCandle = new Candle() { Close = u };
      var dCandle = new Candle() { Close = d };
      avgU.Update(uCandle);
      avgD.Update(dCandle);

      rs = avgU.Value / (avgD.Value == 0 ? 1 : avgD.Value);
      Value = 100 - (100 / (1 + rs));

      if (avgD.Value == 0 && avgU.Value != 0) {
        Value = 100;
      } else if (avgD.Value == 0) {
        Value = 0;
      }

      lastPrice = price;
      age += 1;
    }

    record SerializeState(
      bool Ready,
      double Value,
      int period,
      double u,
      double d,
      double rs,
      int age,
      double lastPrice,
      string avgU,
      string avgD
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializeState(
        Ready,
        Value,
        period,
        u,
        d,
        rs,
        age,
        lastPrice,
        avgU.Serialize(),
        avgD.Serialize()
      ));
    }

    public void Deserialize(string data) {
      var parsed = JsonSerializer.Deserialize<SerializeState>(data);
      if (parsed == null) {
        throw new ArgumentException("Failed to deserialize state");
      }

      Ready = parsed.Ready;
      Value = parsed.Value;
      period = parsed.period;
      u = parsed.u;
      d = parsed.d;
      rs = parsed.rs;
      age = parsed.age;
      lastPrice = parsed.lastPrice;
      avgU = new SMMAIndicator(period);
      avgU.Deserialize(parsed.avgU);
      avgD = new SMMAIndicator(period);
      avgD.Deserialize(parsed.avgD);
    }
  }

  public static RSIIndicator RSI(int period) {
    return new RSIIndicator(period);
  }
}
