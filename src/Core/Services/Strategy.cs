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

  public virtual async Task Preload(IEnumerable<Candle> candles) {
    foreach (var candle in candles) {
      foreach (var indicator in indicators) {
        indicator.Update(candle);
      }
    }
  }

  public virtual async Task<Signal?> Update(Candle candle) {
    foreach (var indicator in indicators) {
      indicator.Update(candle);
    }

    return null;
  }

  public abstract string Serialize();
  public abstract void Deserialize(string data);
}
