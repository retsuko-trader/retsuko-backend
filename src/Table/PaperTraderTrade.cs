using Retsuko.Core;

namespace Retsuko;

public record struct PaperTraderTrade(
  string id,
  string trader_id,
  DateTime ts,
  SignalKind signal,
  double confidence,
  double asset,
  double currency,
  double price,
  double profit
) {
  public static string TableName => "paper_trader_trade";
}
