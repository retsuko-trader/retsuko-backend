using System.Text.Json;
using Retsuko.Core;

namespace Retsuko.Dtos;

public record struct ExtBacktestRun {
  public string id { get; init; }
  public string name { get; init; }
  public string description { get; init; }
  public DateTime created_at { get; init; }
  public DateTime? ended_at { get; init; }
  public BulkBacktestConfig config { get; init; }

  public ExtBacktestRun(BacktestRun run) {
    id = run.id;
    name = run.name;
    description = run.description;
    created_at = run.created_at;
    ended_at = run.ended_at;
    config = new BulkBacktestConfig {
      datasets = JsonSerializer.Deserialize<DatasetConfig[]>(run.datasets)!,
      strategies = JsonSerializer.Deserialize<StrategyConfig[]>(run.strategies)!,
      broker = JsonSerializer.Deserialize<PaperBrokerConfig>(run.broker_config)
    };
  }
}
