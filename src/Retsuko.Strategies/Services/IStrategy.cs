using Retsuko.Strategies.Core;

namespace Retsuko.Strategies.Services;

public record StrategyConsistencyResult(
  bool IsSuccess,
  IList<string> Errors
);

public interface IStrategy: ISerializable {
  Task Preload(Candle candle);

  Task<Signal?> Update(Candle candle);
  StrategyConsistencyResult CheckConsistency();
  Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle);
  object? Dump();
}

public interface IStrategyCreate<T> where T: IStrategyCreate<T> {
  static abstract string Name { get; }
  static abstract string DefaultConfig { get; }
  static abstract T Create(string config);
}
