using Binance.Net.Objects.Models.Futures;

namespace Retsuko.Core;

public record struct Trade(
  DateTime ts,
  SignalKind signal,
  double confidence,
  double asset,
  double currency,
  double price,
  double profit,
  CryptoExchange.Net.Objects.WebCallResult<BinanceUsdFuturesOrder>? order = null
) {
  public double TotalBalance => asset * price + currency;
}
