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
  BinanceFuturesAccountInfoV3 accountInfo,
  BinanceFuturesAccountInfoAsset? assetInfo,
  BinanceFuturesAccountInfoAsset? currencyInfo,
  BinanceFuturesAccountInfoPosition? position,
  WebCallResult<BinanceUsdFuturesOrder>? order
): LiveBrokerEvent;

public record LiveBrokerOrderUpdateEvent(
  LiveTraderOrder rootOrder,
  LiveTraderOrder order,
  WebCallResult<BinanceUsdFuturesOrder>? orderResp
): LiveBrokerEvent;

public record LiveBrokerOrderFilledEvent(
  LiveTraderOrder rootOrder,
  BinanceUsdFuturesOrder order
): LiveBrokerEvent;

public record LiveBrokerOrderDelayedEvent(
  LiveTrader trader,
  Candle candle,
  Signal signal
): LiveBrokerEvent;
