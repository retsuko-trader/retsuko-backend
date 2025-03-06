using DuckDB.NET.Data;

namespace Retsuko;

public class Database {
  public static DuckDBConnection Candle { get; } = CreateConnection("db/candles.db");
  public static DuckDBConnection Backtest { get; } = CreateConnection("db/backtests.db");
  public static DuckDBConnection PaperTrader { get; } = CreateConnection("db/paperTraders.db");


  public static DuckDBConnection CreateConnection(string filename, bool readOnly = false) {
    var connectionString = $"Data Source={filename};";
    if (readOnly) {
      connectionString += "ACCESS_MODE=READ_ONLY";
    }
    var connection = new DuckDBConnection(connectionString);
    connection.Open();

    return connection;
  }

  public static DuckDBConnection CreateCandleDatabase(string symbol, bool readOnly = false) {
    return CreateConnection($"db/candles/{symbol}.db", readOnly);
  }
}
