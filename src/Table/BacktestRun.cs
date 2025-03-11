using System.Text.Json;
using DuckDB.NET.Data;

namespace Retsuko.Core;

public record struct BacktestRun(
  string id,
  string name,
  string description,
  DateTime created_at,
  DateTime? ended_at,
  string datasets,
  string strategies,
  string broker_config
) {
  public static string TableName => "backtest_run";

  public static async Task<IReadOnlyList<BacktestRun>> List() {
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName}";

    using var reader = await command.ExecuteReaderAsync();
    var runs = new List<BacktestRun>();

    while (reader.Read()) {
      var run = new BacktestRun(
        id: reader.GetString(0),
        name: reader.GetString(1),
        description: reader.GetString(2),
        created_at: reader.GetDateTime(3),
        ended_at: reader.IsDBNull(4) ? null : reader.GetDateTime(4),
        datasets: reader.GetString(5),
        strategies: reader.GetString(6),
        broker_config: reader.GetString(7)
      );

      runs.Add(run);
    }

    return runs;
  }

  public static BacktestRun Create(BulkBacktestConfig config) {
    var runId = new Visus.Cuid.Cuid2().ToString();
    var run = new BacktestRun(
      id: runId,
      name: config.name,
      description: config.description,
      created_at: DateTime.UtcNow,
      ended_at: null,
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
      .AppendValue(created_at)
      .AppendValue(ended_at)
      .AppendValue(datasets)
      .AppendValue(strategies)
      .AppendValue(broker_config)
      .EndRow();

    appender.Close();
  }

  public async Task UpdateEnd() {
    ended_at = DateTime.UtcNow;

    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"UPDATE {TableName} SET ended_at = $ended_at WHERE id = $id";
    command.Parameters.Add(new DuckDBParameter("ended_at", ended_at));
    command.Parameters.Add(new DuckDBParameter("id", id));

    await command.ExecuteNonQueryAsync();
  }
}
