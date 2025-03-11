namespace Retsuko.Core;

public record struct BulkBacktestConfig(
  string name,
  string description,
  DatasetConfig[] datasets,
  StrategyConfig[] strategies,
  PaperBrokerConfig broker
) {
}
