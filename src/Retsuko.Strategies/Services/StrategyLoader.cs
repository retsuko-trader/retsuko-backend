namespace Retsuko.Strategies.Services;

public record StrategyInfo(
  string Name,
  string Config,
  Func<string, IStrategy> CreateFn
);

public record ExtStrategyInfo(
  string Name,
  string Config
);

public static class StrategyLoader {
  public static readonly StrategyInfo[] strategies = LoadStrategies();

  private static StrategyInfo[] LoadStrategies() {
    var targetType = typeof(IStrategyCreate<>);

    var strategyTypes = AppDomain.CurrentDomain
      .GetAssemblies()
      .SelectMany(assembly => assembly.GetTypes())
      .Where(type => (
        type.IsClass
        && !type.IsAbstract
        && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == targetType)
      ));

    var strategyInfos = strategyTypes
      .Select(type => {
        var name = type.GetProperty("Name")?.GetValue(null)?.ToString();
        var defaultConfig = type.GetProperty("DefaultConfig")?.GetValue(null)?.ToString();
        var createFn = type.GetMethod("Create");

        if (name == null || defaultConfig == null || createFn == null) {
          return null;
        }

        return new StrategyInfo(name, defaultConfig, config => (IStrategy)createFn.Invoke(null, [config])!);
      })
      .Where(info => info != null)
      .ToArray() as StrategyInfo[];

    return strategyInfos;
  }

  public static IEnumerable<ExtStrategyInfo> GetStrategyEntries() {
    return strategies.Select(strategy => new ExtStrategyInfo(strategy.Name, strategy.Config));
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
