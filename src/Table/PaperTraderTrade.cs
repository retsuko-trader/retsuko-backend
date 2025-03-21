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

  public void Insert() {
    using var appender = Database.PaperTrader.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(trader_id)
      .AppendValue(ts)
      .AppendValue(signal.ToString())
      .AppendValue(confidence)
      .AppendValue(asset)
      .AppendValue(currency)
      .AppendValue(price)
      .AppendValue(profit);

    appender.Close();
  }
}
