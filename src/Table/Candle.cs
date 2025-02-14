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

  public void AppendRow(DuckDB.NET.Data.IDuckDBAppenderRow row) {
    row.AppendValue(market.ToString())
      .AppendValue(symbol)
      .AppendValue(ts)
      .AppendValue(close)
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
