namespace Retsuko.Core;

using System.Threading.Tasks.Dataflow;

public static class LiveCandleDispatcher {
  private static Dictionary<string, BufferBlock<Candle>> dispatchers = new();

  public static BufferBlock<Candle> Register(string id) {
    if (dispatchers.TryGetValue(id, out var dispatcher)) {
      return dispatcher;
    }

    return dispatchers[id] = new BufferBlock<Candle>();
  }
}