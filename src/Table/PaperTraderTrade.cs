using DuckDB.NET.Data;
using Retsuko.Core;

namespace Retsuko;

public record struct PaperTraderTrade(
  string id,
  string traderId,
  DateTime ts,
  SignalKind signal,
  double confidence,
  double asset,
  double currency,
  double price,
  double profit
) {
  public static string TableName => "paper_trader_trade";

  public static async Task<IReadOnlyList<PaperTraderTrade>> List(string traderId) {
    using var command = Database.PaperTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE trader_id = $traderId ORDER BY ts ASC";
    command.Parameters.Add(new DuckDBParameter("traderId", traderId));

    using var reader = await command.ExecuteReaderAsync();
    var trades = new List<PaperTraderTrade>();

    while (reader.Read()) {
      var trade = new PaperTraderTrade(
        id: reader.GetString(0),
        traderId: reader.GetString(1),
        ts: reader.GetDateTime(2),
        signal: Enum.Parse<SignalKind>(reader.GetString(3)),
        confidence: reader.GetDouble(4),
        asset: reader.GetDouble(5),
        currency: reader.GetDouble(6),
        price: reader.GetDouble(7),
        profit: reader.GetDouble(8)
      );

      trades.Add(trade);
    }

    return trades;
  }

  public void Insert() {
    using var appender = Database.PaperTrader.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(traderId)
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
