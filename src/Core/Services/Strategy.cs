namespace Retsuko.Core;

public abstract class Strategy<TConfig>: IStrategy, ISerializable where TConfig: struct {
  public TConfig Config { get; protected set; }

  protected List<IIndicator> indicators;

  public Strategy(TConfig config) {
    this.Config = config;
    this.indicators = new List<IIndicator>();
  }

  protected T AddIndicator<T>(T indicator) where T: IIndicator {
    indicators.Add(indicator);
    return indicator;
  }

  public virtual async Task Preload(Candle candle) {
    foreach (var indicator in indicators) {
      indicator.Update(candle);
    }

    await ValueTask.CompletedTask;
  }

  public virtual async Task<Signal?> Update(Candle candle) {
    foreach (var indicator in indicators) {
      indicator.Update(candle);
    }

    await ValueTask.CompletedTask;
    return null;
  }

  public virtual async Task<IEnumerable<DebugIndicatorInput>> Debug(Candle candle) {
    await ValueTask.CompletedTask;

    return [];
  }

  public abstract string Serialize();
  public abstract void Deserialize(string data);
}
