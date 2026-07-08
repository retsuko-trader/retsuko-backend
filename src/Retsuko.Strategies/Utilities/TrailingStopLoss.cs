using System.Text.Json;
using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Utilities;

public class TrailingStopLoss: ISerializable {
  protected double percentage;

  private double prevPrice;
  private double stopLoss;
  private bool isActive;

  public TrailingStopLoss(double percentage) {
    this.percentage = percentage;
    prevPrice = 0;
    stopLoss = 0;
    isActive = false;
  }

  public void Begin(double price) {
    prevPrice = price;
    stopLoss = (100 - percentage) / 100 * price;
    isActive = true;
  }

  public void End() {
    prevPrice = 0;
    stopLoss = 0;
    isActive = false;
  }

  public bool IsTriggered(double price) {
    if (!isActive) {
      return false;
    }

    return stopLoss > price;
  }

  record SerializedState(
    double percentage,
    double prevPrice,
    double stopLoss,
    bool isActive
  );

  public string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      percentage,
      prevPrice,
      stopLoss,
      isActive
    ));
  }

  public void Deserialize(string data) {
    var parsed = JsonSerializer.Deserialize<SerializedState>(data);
    if (parsed == null) {
      return;
    }

    percentage = parsed.percentage;
    prevPrice = parsed.prevPrice;
    stopLoss = parsed.stopLoss;
    isActive = parsed.isActive;
  }
}
