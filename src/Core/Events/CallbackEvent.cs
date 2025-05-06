using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;

namespace Retsuko.Core.Events;

public record CallbackEvent();

public record CallbackFailEvent(
  string id,
  string symbol,
  KlineInterval interval,
  BinanceStreamKline kline,
  int queueLength,
  CallbackContextKind contextKind,
  Exception? exception
): CallbackEvent;

public enum CallbackContextKind {
  Subscription,
  Manual,
}
