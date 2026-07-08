using System.Text.Json;
using DuckDB.NET.Data;

namespace Retsuko.Core;

public record struct BacktestSingle(
  string id,
  string run_id,
  string dataset,
  DateTime dataset_start,
  DateTime dataset_end,
  string strategy_name,
  string strategy_config,
  string broker_config,
  string metrics
) {
  public static string TableName => "backtest_single";

  public static async Task<IReadOnlyList<BacktestSingle>> List(string runId) {
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE run_id = $runId";
    command.Parameters.Add(new DuckDBParameter("runId", runId));

    using var reader = await command.ExecuteReaderAsync();
    var singles = new List<BacktestSingle>();

    while (await reader.ReadAsync()) {
      var single = new BacktestSingle(
        id: reader.GetString(0),
        run_id: reader.GetString(1),
        dataset: reader.GetString(2),
        dataset_start: reader.GetDateTime(3),
        dataset_end: reader.GetDateTime(4),
        strategy_name: reader.GetString(5),
        strategy_config: reader.GetString(6),
        broker_config: reader.GetString(7),
        metrics: reader.GetString(8)
      );

      singles.Add(single);
    }

    return singles;
  }

  public static BacktestSingle Create(string runId, BacktestConfig config, TraderMetrics metrics) {
    var id = new Visus.Cuid.Cuid2().ToString();

    var single = new BacktestSingle(
      id: id,
      run_id: runId,
      dataset: JsonSerializer.Serialize(config.dataset),
      dataset_start: config.dataset.start,
      dataset_end: config.dataset.end,
      strategy_name: config.strategy.name,
      strategy_config: JsonSerializer.Serialize(config.strategy),
      broker_config: JsonSerializer.Serialize(config.broker),
      metrics: JsonSerializer.Serialize(metrics)
    );

    return single;
  }

  public void Insert() {
    using var appender = Database.Backtest.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(run_id)
      .AppendValue(dataset)
      .AppendValue(dataset_start)
      .AppendValue(dataset_end)
      .AppendValue(strategy_name)
      .AppendValue(strategy_config)
      .AppendValue(broker_config)
      .AppendValue(metrics)
      .EndRow();

    appender.Close();
  }
}
