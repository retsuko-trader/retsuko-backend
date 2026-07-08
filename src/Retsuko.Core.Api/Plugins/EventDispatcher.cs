using Retsuko.Core.Events;

namespace Retsuko.Plugins;

public static class EventDispatcher {
  public static event Action<CallbackEvent> OnCallbackEvent;
  public static event Action<LiveBrokerEvent> OnLiveBrokerEvent;
  public static event Action<HttpContext?, Exception> OnException;

  public static void Event<T>(T eve) {
    if (eve is LiveBrokerEvent lbe) {
      OnLiveBrokerEvent?.Invoke(lbe);
    } else if (eve is CallbackEvent cbe) {
      OnCallbackEvent?.Invoke(cbe);
    }
  }

  public static void Exception(HttpContext? context, Exception e) {
    OnException?.Invoke(context, e);
  }
}
