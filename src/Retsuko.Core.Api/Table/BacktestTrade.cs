using DuckDB.NET.Data;

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

  public static async Task<IEnumerable<BacktestTrade>> List(string singleId) {
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE single_id = $singleId ORDER BY ts ASC";
    command.Parameters.Add(new DuckDBParameter("singleId", singleId));

    using var reader = await command.ExecuteReaderAsync();
    var trades = new List<BacktestTrade>();

    while (reader.Read()) {
      var trade = new BacktestTrade(
        single_id: reader.GetString(0),
        ts: reader.GetDateTime(1),
        signal: reader.GetString(2),
        confidence: reader.GetDouble(3),
        asset: reader.GetDouble(4),
        currency: reader.GetDouble(5),
        price: reader.GetDouble(6),
        profit: reader.GetDouble(7)
      );

      trades.Add(trade);
    }

    return trades;
  }

  public static async Task<IEnumerable<BacktestTrade>> List(string[] singleIds) {
    var ids = string.Join(',', singleIds.Select(x => $"'{x}'").ToArray());
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE single_id IN ({ids}) ORDER BY ts ASC";
    command.Parameters.Add(new DuckDBParameter("singleIds", ids));

    using var reader = await command.ExecuteReaderAsync();
    var trades = new List<BacktestTrade>();

    while (reader.Read()) {
      var trade = new BacktestTrade(
        single_id: reader.GetString(0),
        ts: reader.GetDateTime(1),
        signal: reader.GetString(2),
        confidence: reader.GetDouble(3),
        asset: reader.GetDouble(4),
        currency: reader.GetDouble(5),
        price: reader.GetDouble(6),
        profit: reader.GetDouble(7)
      );

      trades.Add(trade);
    }

    return trades;
  }

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
