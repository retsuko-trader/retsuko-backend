using Retsuko.Strategies;

namespace Retsuko.Core;

public record StrategyInfo(
  string Name,
  string Config,
  Func<string, IStrategy> CreateFn
);

public static class StrategyLoader {
  public static readonly StrategyInfo[] strategies = [
    new StrategyInfo("SuperTrend", SuperTrendStrategy.DefaultConfig, SuperTrendStrategy.Create),
    new StrategyInfo("Turtle", TurtleStrategy.DefaultConfig, TurtleStrategy.Create),
    new StrategyInfo("SuperTrendTurtle", SuperTrendTurtleStrategy.DefaultConfig, SuperTrendTurtleStrategy.Create),
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
