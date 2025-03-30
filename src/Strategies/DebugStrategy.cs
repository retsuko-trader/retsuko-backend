namespace Retsuko.Strategies;

using System.Text.Json;
using Retsuko.Core;

public record struct DebugStrategyConfig();

public class DebugStrategy: Strategy<DebugStrategyConfig>, IStrategyCreate<DebugStrategy> {
  public bool direction = false;

  public static string Name => "Debug";
  public static string DefaultConfig => "{}";

  public static DebugStrategy Create(string config) {
    return new DebugStrategy(new ());
  }

  public DebugStrategy(DebugStrategyConfig config): base(config) {
  }

  public override async Task<Signal?> Update(Candle candle) {
    await base.Update(candle);

    if (direction) {
      direction = false;
      return Signal.closeLong;
    } else {
      direction = true;
      return Signal.openLong;
    }
  }

  record SerializedState(bool direction);

  public override string Serialize() {
    return JsonSerializer.Serialize(new SerializedState(
      direction
    ));
  }

  public override void Deserialize(string state) {
    var x = JsonSerializer.Deserialize<SerializedState>(state);
    if (x == null) {
      return;
    }

    direction = x.direction;
  }
}
