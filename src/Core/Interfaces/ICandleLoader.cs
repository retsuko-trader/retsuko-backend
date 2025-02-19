public interface ICandleLoader {
  public Task<bool> Init();

  public Task<bool> Read();
  public Task<Candle> LoadOne();
}
