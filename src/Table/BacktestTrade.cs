namespace Retsuko.Core;

public record struct BacktestTrade(
  string single_id,
  DateTime ts,
  string signal,
  double confidence,
  double asset,
  double currency,
  double price,
  double profit
) {
  public static string TableName => "backtest_trade";

  public static BacktestTrade Create(string singleId, Trade trade) {
    return new BacktestTrade(
      single_id: singleId,
      ts: trade.ts,
      signal: trade.signal.ToString(),
      confidence: trade.confidence,
      asset: trade.asset,
      currency: trade.currency,
      price: trade.price,
      profit: trade.profit
    );
  }

  public static void InsertBulk(IEnumerable<BacktestTrade> trades) {
    using var appender = Database.Backtest.CreateAppender(TableName);

    foreach (var trade in trades) {
      var row = appender.CreateRow();
      row.AppendValue(trade.single_id)
        .AppendValue(trade.ts)
        .AppendValue(trade.signal)
        .AppendValue(trade.confidence)
        .AppendValue(trade.asset)
        .AppendValue(trade.currency)
        .AppendValue(trade.price)
        .AppendValue(trade.profit)
        .EndRow();
    }

    appender.Close();
  }
}
