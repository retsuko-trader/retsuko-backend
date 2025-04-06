using Retsuko.Core.Events;

namespace Retsuko.Plugins;

public static class EventDispatcher {
  public static event Action<LiveBrokerEvent> OnLiveBrokerEvent;
  public static event Action<HttpContext?, Exception> OnException;

  static EventDispatcher() {
    Discord.Initialize();
  }

  public static void Event<T>(T eve) {
    if (eve is LiveBrokerEvent lbe) {
      OnLiveBrokerEvent?.Invoke(lbe);
    }
  }

  public static void Exception(HttpContext? context, Exception e) {
    OnException?.Invoke(context, e);
  }
}
