using Retsuko.Clients;

namespace Retsuko.Core;

public static class StrategyLoader {
  public static async Task<IEnumerable<GStrategy>> GetStrategyEntries() {
    var strategies = await StrategyClient.loaderClient.GetStrategiesAsync(new());
    return strategies.Strategies;
  }

  public static async Task<string?> GetDefaultConfig(string name) {
    var strategies = await StrategyClient.loaderClient.GetStrategiesAsync(new());

    var strategy = strategies.Strategies.FirstOrDefault(strategy => strategy.Name == name);
    if (strategy == null) {
      return null;
    }

    return strategy.Config;
  }
}
