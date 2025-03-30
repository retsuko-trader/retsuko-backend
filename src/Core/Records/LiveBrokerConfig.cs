namespace Retsuko.Core;

public record struct LiveBrokerConfig(
  bool isTestNet,
  int leverege,
  float ratio
);
