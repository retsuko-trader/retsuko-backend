using DuckDB.NET.Data;

namespace Retsuko;

public class Database {
  public static DuckDBConnection Candle { get; } = CreateConnection("db/candles.db");
  public static DuckDBConnection Backtest { get; } = CreateConnection("db/backtests.db");
  public static DuckDBConnection PaperTrader { get; } = CreateConnection("db/paperTraders.db");


  private static DuckDBConnection CreateConnection(string filename) {
    var connection = new DuckDBConnection($"Data Source={filename}");
    connection.Open();

    return connection;
  }
}
