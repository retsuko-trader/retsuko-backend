namespace Retsuko.Core;

public interface IStrategy: ISerializable {
  Task Preload(Candle candle);

  Task<Signal?> Update(Candle candle);
  Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle);
}

public interface IStrategyCreate<T> where T: IStrategyCreate<T> {
  static abstract string Name { get; }
  static abstract string DefaultConfig { get; }
  static abstract T Create(string config);
}
