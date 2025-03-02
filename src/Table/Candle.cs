using Binance.Net.Enums;

namespace Retsuko;

public enum Market {
  futures,
  spot,
}

public record struct Candle(
  Market market,
  string symbol,
  KlineInterval interval,
  DateTime ts,
  double open,
  double high,
  double low,
  double close,
  double volume
) {
  public static string TableName => "candle";

  public static Candle From(Market market, string symbol, KlineInterval interval, Binance.Net.Interfaces.IBinanceKline kline) {
    return new Candle(
      market,
      symbol,
      interval,
      kline.OpenTime,
      (double)kline.OpenPrice,
      (double)kline.HighPrice,
      (double)kline.LowPrice,
      (double)kline.ClosePrice,
      (double)kline.Volume
    );
  }


  public static Candle From(Market market, string symbol, KlineInterval interval, System.Data.Common.DbDataReader reader) {
    return new Candle(
      market,
      symbol,
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
    row.AppendValue(market.ToString())
      .AppendValue(symbol)
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
    var command = Database.Candle.CreateCommand();

    command.CommandText = "SELECT market, symbol, interval, min(ts), max(ts), count(ts) FROM candle GROUP BY market, symbol, interval";
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

    return result;
  }
}

public record struct Dataset(
  Market market,
  string symbol,
  int interval,
  DateTime start,
  DateTime end,
  int count
) {
}
