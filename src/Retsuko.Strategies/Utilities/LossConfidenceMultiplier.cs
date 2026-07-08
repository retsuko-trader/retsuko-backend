using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Utilities;

public class LossConfidenceMultiplier: ISerializable {
  private int maxContinuousLosses;
  private float maxLossConfidenceMultiplier;

  private int continuousLosses;
  private double buyPrice;

  public LossConfidenceMultiplier(int maxContinuousLosses, float maxLossConfidenceMultiplier) {
    this.maxContinuousLosses = maxContinuousLosses;
    this.maxLossConfidenceMultiplier = maxLossConfidenceMultiplier;
  }

  public void Buy(double price) {
    if (buyPrice != 0) {
      return;
    }

    buyPrice = price;
  }

  public void Sell(double price) {
    if (buyPrice == 0) {
      return;
    }

    if (price < buyPrice) {
      continuousLosses += 1;
    } else {
      continuousLosses = 0;
    }

    buyPrice = 0;
    continuousLosses = Math.Min(continuousLosses, maxContinuousLosses);
  }

  public float GetMultiplier() {
    if (continuousLosses == 0) {
      return 1;
    }

    var i = 1f - (maxContinuousLosses - continuousLosses) / (float)maxContinuousLosses;
    return 1f - (1f - maxLossConfidenceMultiplier) * i;
  }

  record SerializedState(
    int max,
    float multiplier,
    int continuousLosses,
    double buyPrice
  );

  public string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      maxContinuousLosses,
      maxLossConfidenceMultiplier,
      continuousLosses,
      buyPrice
    ));
  }

  public void Deserialize(string data) {
    var parsed = JsonSerializer.Deserialize<SerializedState>(data);
    if (parsed == null) {
      return;
    }

    maxContinuousLosses = parsed.max;
    maxLossConfidenceMultiplier = parsed.multiplier;
    continuousLosses = parsed.continuousLosses;
    buyPrice = parsed.buyPrice;
  }
}
