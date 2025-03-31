namespace Retsuko.Core;

public static class LiveOrderTracker {
  public static void StartTrack(LiveTraderOrder order) {
    _ = Task.Run(async () => {
      order.Insert();

      var orderId = order.orderId;

      // TODO: call api to get order status, until it is closed
    });
  }
}
