using System.Text.Json;
using Retsuko.Core;

namespace Retsuko.Dtos;

public record struct ExtBacktestSingle {
  public string id { get; init; }
  public string run_id { get; init; }
  public DateTime dataset_start { get; init; }
  public DateTime dataset_end { get; init; }
  public BacktestConfig config { get; init; }
  public TraderMetrics metrics { get; init; }

  public ExtBacktestSingle(BacktestSingle single) {
    id = single.id;
    run_id = single.run_id;
    dataset_start = single.dataset_start;
    dataset_end = single.dataset_end;
    config = new BacktestConfig(
      JsonSerializer.Deserialize<DatasetConfig>(single.dataset),
      JsonSerializer.Deserialize<StrategyConfig>(single.strategy_config),
      JsonSerializer.Deserialize<PaperBrokerConfig>(single.broker_config)
    );
    metrics = JsonSerializer.Deserialize<TraderMetrics>(single.metrics);
  }
}
