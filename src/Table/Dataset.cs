using Binance.Net.Enums;
using DuckDB.NET.Data;

namespace Retsuko;

public record struct Dataset(
  Market market,
  int symbolId,
  int interval,
  DateTime start,
  DateTime end,
  int count
) {
  public static string TableName => "dataset";

  public static async Task<List<Dataset>> List() {
    using var command = Database.Candle.CreateCommand();

    command.CommandText = @"SELECT market, symbolId, interval, start, ""end"", count FROM dataset ORDER BY market asc, symbolId asc, interval";
    var reader = await command.ExecuteReaderAsync();
    var result = new List<Dataset>();
    while (reader.Read()) {
      var market = reader.GetInt32(0);
      var symbolId = reader.GetInt32(1);
      var interval = reader.GetInt32(2);
      var start = reader.GetDateTime(3);
      var end = reader.GetDateTime(4);
      var count = reader.GetInt32(5);

      var dataset = new Dataset((Market)market, symbolId, interval, start, end, count);
      result.Add(dataset);
    }

    return result;
  }

  public static async Task Upsert(Dataset dataset) {
    using var command = Database.Candle.CreateCommand();

    command.CommandText = $@"INSERT INTO {TableName} (market, symbolId, interval, start, ""end"", count)
      VALUES ($market, $symbolId, $interval, $start, $end, $count)
      ON CONFLICT (market, symbolId, interval) DO UPDATE SET
        start = excluded.start,
        ""end"" = excluded.""end"",
        count = excluded.count";
    command.Parameters.Add(new DuckDBParameter("market", (int)dataset.market));
    command.Parameters.Add(new DuckDBParameter("symbolId", dataset.symbolId));
    command.Parameters.Add(new DuckDBParameter("interval", dataset.interval));
    command.Parameters.Add(new DuckDBParameter("start", dataset.start));
    command.Parameters.Add(new DuckDBParameter("end", dataset.end));
    command.Parameters.Add(new DuckDBParameter("count", dataset.count));
    await command.ExecuteNonQueryAsync();
  }

  public static async Task<Dataset> GetFrom(DuckDBConnection db, KlineInterval interval) {
    using var command = db.CreateCommand();

    command.CommandText = "SELECT market, symbolId, interval, min(ts), max(ts), count(ts) FROM candle WHERE interval = $interval GROUP BY market, symbolId, interval";
    command.Parameters.Add(new DuckDBParameter("interval", (int)interval));
    var reader = await command.ExecuteReaderAsync();
    if (!reader.Read()) {
      return default;
    }

    var market = reader.GetInt32(0);
    var symbolId = reader.GetInt32(1);
    var interval0 = reader.GetInt32(2);
    var start = reader.GetDateTime(3);
    var end = reader.GetDateTime(4);
    var count = reader.GetInt32(5);

    return new Dataset((Market)market, symbolId, interval0, start, end, count);
  }
}
