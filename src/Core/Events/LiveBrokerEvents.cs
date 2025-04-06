using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;

namespace Retsuko.Core.Events;

public record LiveBrokerEvent();

public record LiveBrokerGotSignalEvent(
  LiveTrader trader,
  LiveBroker broker,
  DateTime ts,
  Symbol symbol,
  Signal signal,
  BinanceFuturesAccountInfoAsset? assetInfo,
  BinanceFuturesAccountInfoAsset? currencyInfo,
  BinanceFuturesAccountInfoPosition? position,
  WebCallResult<BinanceUsdFuturesOrder>? order
): LiveBrokerEvent;
