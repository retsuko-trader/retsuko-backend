using DuckDB.NET.Data;
using Retsuko.Core;

namespace Retsuko;

public record struct LiveTraderTrade(
  string id,
  string traderId,
  DateTime ts,
  SignalKind signal,
  double confidence,
  long? orderId,
  double asset,
  double currency,
  double price,
  double profit
) {
  public static string TableName => "live_trader_trade";

  public static async Task<IReadOnlyList<LiveTraderTrade>> List(string traderId) {
    using var command = Database.PaperTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE trader_id = $traderID ORDER BY ts ASC";
    command.Parameters.Add(new DuckDBParameter("traderId", traderId));

    using var reader = await command.ExecuteReaderAsync();
    var trades = new List<LiveTraderTrade>();

    while (reader.Read()) {
      var trade = new LiveTraderTrade(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetDateTime(2),
        (SignalKind)reader.GetInt32(3),
        reader.GetDouble(4),
        reader.IsDBNull(5) ? null : reader.GetInt64(5),
        reader.GetDouble(6),
        reader.GetDouble(7),
        reader.GetDouble(8),
        reader.GetDouble(9)
      );
      trades.Add(trade);
    }

    await reader.CloseAsync();
    return trades;
  }

  public void Insert() {
    using var appender = Database.LiveTrader.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(traderId)
      .AppendValue(ts)
      .AppendValue(signal.ToString())
      .AppendValue(confidence)
      .AppendValue(orderId)
      .AppendValue(asset)
      .AppendValue(currency)
      .AppendValue(price)
      .AppendValue(profit)
      .EndRow();

    appender.Close();
  }
}
