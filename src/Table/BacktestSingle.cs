using System.Text.Json;

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
