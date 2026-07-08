namespace Retsuko.Core;

public record struct BacktestConfig(
  DatasetConfig dataset,
  StrategyConfig strategy,
  PaperBrokerConfig broker
) {
}
