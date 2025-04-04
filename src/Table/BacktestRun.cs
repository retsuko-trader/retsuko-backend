using System.Text.Json;
using DuckDB.NET.Data;

namespace Retsuko.Core;

public record struct BacktestRun(
  string id,
  string name,
  string description,
  DateTime createdAt,
  DateTime? endedAt,
  string datasets,
  string strategies,
  string broker_config
) {
  public static string TableName => "backtest_run";

  public static async Task<IReadOnlyList<BacktestRun>> List() {
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} ORDER BY created_at DESC";

    using var reader = await command.ExecuteReaderAsync();
    var runs = new List<BacktestRun>();

    while (await reader.ReadAsync()) {
      var run = new BacktestRun(
        id: reader.GetString(0),
        name: reader.GetString(1),
        description: reader.GetString(2),
        createdAt: reader.GetDateTime(3),
        endedAt: reader.IsDBNull(4) ? null : reader.GetDateTime(4),
        datasets: reader.GetString(5),
        strategies: reader.GetString(6),
        broker_config: reader.GetString(7)
      );

      runs.Add(run);
    }

    return runs;
  }

  public static async Task<BacktestRun?> Get(string id) {
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE id = $id";
    command.Parameters.Add(new DuckDBParameter("id", id));

    using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) {
      return null;
    }

    return new BacktestRun(
      id: reader.GetString(0),
      name: reader.GetString(1),
      description: reader.GetString(2),
      createdAt: reader.GetDateTime(3),
      endedAt: reader.IsDBNull(4) ? null : reader.GetDateTime(4),
      datasets: reader.GetString(5),
      strategies: reader.GetString(6),
      broker_config: reader.GetString(7)
    );
  }

  public static BacktestRun Create(BulkBacktestConfig config) {
    var runId = new Visus.Cuid.Cuid2().ToString();
    var run = new BacktestRun(
      id: runId,
      name: config.name,
      description: config.description,
      createdAt: DateTime.UtcNow,
      endedAt: null,
      datasets: JsonSerializer.Serialize(config.datasets),
      strategies: JsonSerializer.Serialize(config.strategies),
      broker_config: JsonSerializer.Serialize(config.broker)
    );

    return run;
  }

  public void Insert() {
    using var appender = Database.Backtest.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(name)
      .AppendValue(description)
      .AppendValue(createdAt)
      .AppendValue(endedAt)
      .AppendValue(datasets)
      .AppendValue(strategies)
      .AppendValue(broker_config)
      .EndRow();

    appender.Close();
  }

  public async Task UpdateEnd() {
    endedAt = DateTime.UtcNow;

    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"UPDATE {TableName} SET ended_at = $ended_at WHERE id = $id";
    command.Parameters.Add(new DuckDBParameter("ended_at", endedAt));
    command.Parameters.Add(new DuckDBParameter("id", id));

    await command.ExecuteNonQueryAsync();
  }
}
