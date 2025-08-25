using Binance.Net.Enums;
using DuckDB.NET.Data;

namespace Retsuko;

public enum Market {
  futures,
  spot,
}

public record struct Candle(
  Market market,
  int symbolId,
  KlineInterval interval,
  DateTime ts,
  double open,
  double high,
  double low,
  double close,
  double volume
) {
  public static string TableName => "candle";

  public static Candle From(Market market, int symbolId, KlineInterval interval, Binance.Net.Interfaces.IBinanceKline kline) {
    return new Candle(
      market,
      symbolId,
      interval,
      kline.OpenTime,
      (double)kline.OpenPrice,
      (double)kline.HighPrice,
      (double)kline.LowPrice,
      (double)kline.ClosePrice,
      (double)kline.Volume
    );
  }


  public static Candle From(Market market, int symbolId, KlineInterval interval, System.Data.Common.DbDataReader reader) {
    return new Candle(
      market,
      symbolId,
      interval,
      reader.GetDateTime(3),
      reader.GetDouble(4),
      reader.GetDouble(5),
      reader.GetDouble(6),
      reader.GetDouble(7),
      reader.GetDouble(8)
    );
  }

  public void AppendRow(DuckDB.NET.Data.IDuckDBAppenderRow row) {
    row.AppendValue((int)market)
      .AppendValue(symbolId)
      .AppendValue((int)interval)
      .AppendValue(ts)
      .AppendValue(open)
      .AppendValue(high)
      .AppendValue(low)
      .AppendValue(close)
      .AppendValue(volume)
      .EndRow();
  }

  public static async Task<List<Dataset>> GetDataset() {
    var symbols = await Symbol.List();

    var results = new List<Dataset>();

    await Parallel.ForEachAsync(symbols, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (symbol, t) => {
      var datasets = await GetDataset(symbol.name);

      lock (results) {
        results.AddRange(datasets);
      }
    });

    results.Sort((a, b) => {
      var cmp = a.market.CompareTo(b.market);
      if (cmp != 0) {
        return cmp;
      }

      cmp = a.symbolId.CompareTo(b.symbolId);
      if (cmp != 0) {
        return cmp;
      }

      return a.interval.CompareTo(b.interval);
    });

    return results;
  }

  public static async Task<List<Dataset>> GetDataset(string symbol) {
    using var db = Database.CreateCandleDatabase(symbol);
    using var command = db.CreateCommand();

    command.CommandText = "SELECT market, symbolId, interval, min(ts), max(ts), count(ts) FROM candle GROUP BY market, symbolId, interval";
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

  public static async Task<IReadOnlyList<Candle>> List(int symbolId, KlineInterval interval, DateTime? start, DateTime? end, float? sampleRate) {
    var symbol = await Symbol.Get(symbolId);
    if (symbol == null) {
      throw new ArgumentException($"Symbol {symbolId} not found");
    }

    using var db = Database.CreateCandleDatabase(symbol.Value.name);
    using var command = db.CreateCommand();

    if (sampleRate.HasValue) {
      command.CommandText = $"SELECT * FROM candle WHERE interval = $interval AND ts >= $start AND ts <= $end USING SAMPLE {sampleRate * 100}% (system, 1) ORDER BY ts ASC";
    } else {
      command.CommandText = $"SELECT * FROM candle WHERE interval = $interval AND ts >= $start AND ts <= $end ORDER BY ts ASC";
    }

    command.Parameters.Add(new DuckDBParameter("interval", (int)interval));
    command.Parameters.Add(new DuckDBParameter("start", (start ?? DateTimeOffset.MinValue).DateTime));
    command.Parameters.Add(new DuckDBParameter("end", (end ?? DateTimeOffset.MaxValue).DateTime));

    var reader = await command.ExecuteReaderAsync();
    var candles = new List<Candle>();

    while (reader.Read()) {
      var candle = From(Market.futures, symbolId, interval, reader);
      candles.Add(candle);
    }

    return candles;
  }

  public static async Task<Candle?> GetFirstBetween(DuckDBConnection db, KlineInterval interval, DateTime start, DateTime end) {
    using var command = db.CreateCommand();

    command.CommandText = $"SELECT * FROM candle WHERE interval = $interval AND ts >= $start AND ts < $end ORDER BY ts ASC LIMIT 1";
    command.Parameters.Add(new DuckDBParameter("interval", (int)interval));
    command.Parameters.Add(new DuckDBParameter("start", start));
    command.Parameters.Add(new DuckDBParameter("end", end));

    var reader = await command.ExecuteReaderAsync();
    if (reader.Read()) {
      return From(Market.futures, -1, interval, reader);
    } else {
      return null;
    }
  }
}
