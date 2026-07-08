namespace Retsuko.Core;

public interface ICandleLoader {
  public Task<bool> Init();

  public Task<IEnumerable<Candle>> Preload();

  public Task<bool> Read();
  public Task<Candle> LoadOne();
}
