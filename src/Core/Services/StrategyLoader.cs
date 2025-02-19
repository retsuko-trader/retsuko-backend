record StrategyInfo(
  string Name,
  string Config,
  Func<string, IStrategy> CreateFn
);

public static class StrategyLoader {
  private static readonly StrategyInfo[] strategies = [
    new StrategyInfo("SuperTrend", SuperTrendStrategy.DefaultConfig, SuperTrendStrategy.Create),
  ];

  public static IEnumerable<string> GetStrategyNames() {
    return strategies.Select(strategy => strategy.Name);
  }

  public static string? GetDefaultConfig(string name) {
    var strategy = strategies.FirstOrDefault(strategy => strategy.Name == name);
    if (strategy == null) {
      return null;
    }

    return strategy.Config;
  }

  public static IStrategy? CreateStrategy(string name, string config) {
    var strategy = strategies.FirstOrDefault(strategy => strategy.Name == name);
    if (strategy == null) {
      return null;
    }

    return strategy.CreateFn(config);
  }
}
