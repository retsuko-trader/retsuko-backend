namespace Retsuko.Core;

public record struct BulkBacktestConfig(
  DatasetConfig[] datasets,
  StrategyConfig[] strategies,
  PaperBrokerConfig broker
) {
}
