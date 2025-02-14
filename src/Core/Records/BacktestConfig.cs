public record struct BacktestConfig(
  DatasetConfig dataset,
  StrategyConfig strategy,
  PaperBrokerConfig broker
) {
}
