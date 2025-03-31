using Binance.Net.Enums;

namespace Retsuko.Core;

public record struct LiveTraderDatasetConfig(
  Market market,
  int symbolId,
  KlineInterval interval,
  int preloadCount
): IPreloadableDatasetConfig;

public record struct LiveTraderCreateConfig(
  string name,
  string description
);

public record struct LiveTraderConfig(
  LiveTraderCreateConfig info,
  LiveTraderDatasetConfig dataset,
  StrategyConfig strategy,
  LiveBrokerConfig broker
);
