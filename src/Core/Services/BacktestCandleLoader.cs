using System.Data.Common;
using DuckDB.NET.Data;
using Retsuko;

public class BacktestCandleLoader: ICandleLoader {
  private DatasetConfig config;
  private DbDataReader reader;

  public BacktestCandleLoader(DatasetConfig config) {
    this.config = config;
  }

  public async Task<bool> Init() {

    var command = Database.Candle.CreateCommand();
    command.UseStreamingMode = true;

    command.CommandText = "SELECT ts, open, high, low, close, volume FROM candle WHERE market = $market AND symbol = $symbol AND interval = $interval AND ts BETWEEN $start AND $end ORDER BY ts ASC";
    command.Parameters.Add(new DuckDBParameter("market", config.market.ToString()));
    command.Parameters.Add(new DuckDBParameter("symbol", config.symbol));
    command.Parameters.Add(new DuckDBParameter("interval", config.interval));
    command.Parameters.Add(new DuckDBParameter("start", config.start));
    command.Parameters.Add(new DuckDBParameter("end", config.end));

    reader = await command.ExecuteReaderAsync();
    return true;
  }

  public async Task<bool> Read() {
    return await reader.ReadAsync();
  }

  public async Task<Candle> LoadOne() {
    return Candle.From(config.market, config.symbol, config.interval, reader);
  }
}
