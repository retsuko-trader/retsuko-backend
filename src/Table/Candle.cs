public enum Market {
  futures,
  spot,
}

public record struct Candle(
  Market market,
  string symbol,
  DateTime ts,
  double open,
  double high,
  double low,
  double close,
  double volume
) {

  public static Candle From(Market market, string symbol, Binance.Net.Interfaces.IBinanceKline kline) {
    return new Candle(
      market,
      symbol,
      kline.CloseTime,
      (double)kline.OpenPrice,
      (double)kline.HighPrice,
      (double)kline.LowPrice,
      (double)kline.ClosePrice,
      (double)kline.Volume
    );
  }


  public static Candle From(Market market, string symbol, System.Data.Common.DbDataReader reader) {
    return new Candle(
      market,
      symbol,
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
      .AppendValue(ts)
      .AppendValue(open)
      .AppendValue(high)
      .AppendValue(low)
      .AppendValue(close)
      .AppendValue(volume)
      .EndRow();
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
