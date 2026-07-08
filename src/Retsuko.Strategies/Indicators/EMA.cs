using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Indicators;

public partial class Indicator {
  public class EMAIndicator : IIndicator {
    public bool Ready { get; protected set; }
    public double Value { get; protected set; }

    private double weight;

    public EMAIndicator(double weight) {
      this.weight = weight;
      Ready = false;
      Value = 0;
    }

    public void Update(Candle candle) {
      if (!Ready) {
        Value = candle.Close;
        Ready = true;
      } else {
        var k = 2 / (weight + 1);
        var y = Value;
        Value = candle.Close * k + y * (1 - k);
      }
    }

    protected internal record SerializedState(
      bool ready,
      double value,
      double weight
    );

    public string Serialize() {
      return JsonSerializer.Serialize(new SerializedState(
        Ready,
        Value,
        weight
      ));
    }

    public void Deserialize(string data) {
      var parsed = JsonSerializer.Deserialize<SerializedState>(data);
      if (parsed == null) {
        throw new ArgumentException("Failed to deserialize state");
      }

      Ready = parsed.ready;
      Value = parsed.value;
      weight = parsed.weight;
    }
  }

  public static EMAIndicator EMA(double weight) {
    return new EMAIndicator(weight);
  }
}
