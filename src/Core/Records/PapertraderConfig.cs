using Binance.Net.Enums;

namespace Retsuko.Core;

public record struct PapertraderDatasetConfig(
  Market market,
  int symbolId,
  KlineInterval interval,
  int preloadCount
): IPreloadableDatasetConfig;

public record struct PapertraderCreateConfig(
  string name,
  string description
);

public record struct PapertraderConfig(
  PapertraderCreateConfig info,
  PapertraderDatasetConfig dataset,
  StrategyConfig strategy,
  PaperBrokerConfig broker
) {
}
