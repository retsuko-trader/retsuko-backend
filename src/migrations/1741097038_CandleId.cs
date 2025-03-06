using DuckDB.NET.Data;
using Retsuko.Core;

namespace Retsuko.Migrations;

public static partial class Migrations {
  /*
  public static async Task MigrateCandleV2() {
    var candle2 = Database.CreateConnection("db/candles2.db");

    var command = candle2.CreateCommand();

    command.CommandText = "CREATE TABLE IF NOT EXISTS symbol (id INTEGER PRIMARY KEY, name TEXT)";
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS candle (
      market INT,
      symbolId INT,
      interval INT,
      ts TIMESTAMP,
      open DOUBLE,
      high DOUBLE,
      low DOUBLE,
      close DOUBLE,
      volume DOUBLE,
      PRIMARY KEY (market, symbolId, interval, ts)
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = "CREATE INDEX IF NOT EXISTS candle_symbol_index ON candle (market, symbolId, interval)";
    await command.ExecuteNonQueryAsync();

    command.CommandText = "ATTACH 'db/candles.db' AS old";
    await command.ExecuteNonQueryAsync();

    var info = await Broker.API.ExchangeData.GetExchangeInfoAsync();

    var symbols = info.Data.Symbols.ToArray();
    using (var appender = candle2.CreateAppender("symbol")) {
      var i = 0;
      foreach (var symbol in symbols) {
        var row = appender.CreateRow();
        row.AppendValue(i)
          .AppendValue(symbol.Name)
          .EndRow();

        i += 1;
      }
    }

    command.CommandText = "SELECT market, symbol, interval, min(ts), max(ts), count(ts) FROM old.candle GROUP BY market, symbol, interval";
    var reader = await command.ExecuteReaderAsync();
    var result = new List<Dataset>();
    while (reader.Read()) {
      var market = reader.GetString(0);
      var symbol = reader.GetString(1);
      var interval = reader.GetInt32(2);
      var start = reader.GetDateTime(3);
      var end = reader.GetDateTime(4);
      var count = reader.GetInt32(5);

      var dataset = new Dataset(Enum.Parse<Market>(market), symbol, interval, start, end, count);
      result.Add(dataset);
    }

    foreach (var dataset in result) {
      command.CommandText = @"INSERT INTO candle (market, symbolId, interval, ts, open, high, low, close, volume)
        SELECT
          CASE WHEN market = 'spot' THEN 1 ELSE 0 END,
          (SELECT id FROM symbol WHERE symbol.name = old.candle.symbol),
          interval,
          ts,
          open,
          high,
          low,
          close,
          volume
        FROM old.candle
        WHERE market = $market AND symbol = $symbol AND interval = $interval";
      command.Parameters.Add(new DuckDBParameter("market", dataset.market.ToString()));
      command.Parameters.Add(new DuckDBParameter("symbol", dataset.symbol));
      command.Parameters.Add(new DuckDBParameter("interval", dataset.interval));
      await command.ExecuteNonQueryAsync();

      Console.WriteLine($"Migrated {dataset.market} {dataset.symbol} {dataset.interval}");
    }

    command.CommandText = "DETACH old";
    await command.ExecuteNonQueryAsync();
  }
  */

  public static async Task MigrateCandleV3() {
    var symbols = new List<string>();

    using (var db = Database.CreateConnection("db/candles.db")) {
      var command = db.CreateCommand();
      command.CommandText = "SELECT name FROM symbol ORDER BY id ASC";
      var reader = await command.ExecuteReaderAsync();
      while (reader.Read()) {
        symbols.Add(reader.GetString(0));
      }
    }

    var i = 0;
    foreach (var symbol in symbols) {
      using var db = Database.CreateCandleDatabase(symbol);
      using var command = db.CreateCommand();

      command.CommandText = @"CREATE TABLE IF NOT EXISTS candle (
        market INT,
        symbolId INT,
        interval INT,
        ts TIMESTAMP,
        open DOUBLE,
        high DOUBLE,
        low DOUBLE,
        close DOUBLE,
        volume DOUBLE,
        PRIMARY KEY (interval, ts)
      )";
      await command.ExecuteNonQueryAsync();

      command.CommandText = "CREATE INDEX IF NOT EXISTS candle_symbol_index ON candle (market, symbolId, interval)";
      await command.ExecuteNonQueryAsync();

      command.CommandText = "ATTACH 'db/candles.db' AS old (READ_ONLY)";
      await command.ExecuteNonQueryAsync();

      command.CommandText = @"INSERT INTO candle (market, symbolId, interval, ts, open, high, low, close, volume)
        SELECT
          market,
          symbolId,
          interval,
          ts,
          open,
          high,
          low,
          close,
          volume
        FROM old.candle
        WHERE symbolId = $symbolId";
      command.Parameters.Add(new DuckDBParameter("symbolId", i));
      await command.ExecuteNonQueryAsync();

      command.CommandText = "DETACH old";
      await command.ExecuteNonQueryAsync();

      Console.WriteLine($"Migrated {symbol} ({i + 1}/{symbols.Count})");
      i += 1;
    }
  }
}
