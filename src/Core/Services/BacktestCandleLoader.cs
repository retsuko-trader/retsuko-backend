namespace Retsuko.Core;

using System.Data.Common;
using DuckDB.NET.Data;
using Retsuko;

public class BacktestCandleLoader: ICandleLoader, IDisposable {
  private DatasetConfig config;
  private DuckDBConnection db;
  private DbDataReader reader;

  public BacktestCandleLoader(DatasetConfig config) {
    this.config = config;
  }

  public async Task<IEnumerable<Candle>> Preload() {
    await ValueTask.CompletedTask;
    return [];
  }

  public async Task<bool> Init() {
    var symbol = await Symbol.Get(config.symbolId);
    if (symbol == null) {
      return false;
    }

    db = Database.CreateCandleDatabase(symbol.Value.name);

    using var command = db.CreateCommand();
    command.UseStreamingMode = true;

    command.CommandText = "SELECT * FROM candle WHERE market = $market AND symbolId = $symbolId AND interval = $interval AND ts BETWEEN $start AND $end ORDER BY ts ASC";
    command.Parameters.Add(new DuckDBParameter("market", (int)config.market));
    command.Parameters.Add(new DuckDBParameter("symbolId", config.symbolId));
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
    await ValueTask.CompletedTask;
    return Candle.From(config.market, config.symbolId, config.interval, reader);
  }

  public async IAsyncEnumerable<IEnumerable<Candle>> BatchLoad(int chunkSize) {
    var list = new Queue<Candle>(chunkSize);
    while (await Read()) {
      list.Enqueue(await LoadOne());

      if (list.Count >= chunkSize) {
        yield return list.ToArray();
        list.Clear();
      }
    }

    if (list.Count > 0) {
      yield return list.ToArray();
    }
  }

  public void Dispose() {
    db.Dispose();
    GC.SuppressFinalize(this);
  }
}
