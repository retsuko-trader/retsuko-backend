namespace Retsuko.Core;

public record struct LiveBrokerConfig(
  bool isTestNet,
  int leverage,
  float ratio
);
