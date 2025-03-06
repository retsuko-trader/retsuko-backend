using Binance.Net.Enums;

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
      reader.GetDateTime(0),
      reader.GetDouble(1),
      reader.GetDouble(2),
      reader.GetDouble(3),
      reader.GetDouble(4),
      reader.GetDouble(5)
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
    using var db = Database.CreateCandleDatabase(symbol, true);
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
}
