namespace Retsuko.Core;

public class PreloadCandleLoader: ICandleLoader {
  private IPreloadableDatasetConfig config;

  public PreloadCandleLoader(IPreloadableDatasetConfig config) {
    this.config = config;
  }

  public async Task<bool> Init() {
    await ValueTask.CompletedTask;
    return true;
  }

  public async Task<IEnumerable<Candle>> Preload() {
    var symbol = await Symbol.Get(config.symbolId);

    if (config.preloadCount <= 0) {
      return [];
    }

    var preloadCandles = Exchanger.GetRecentKlinesAsync(symbol.Value.name, config.interval, config.preloadCount);
    var candles = await preloadCandles.ToListAsync();
    return candles.Select(x => Candle.From(config.market, symbol.Value.id, config.interval, x));
  }

  public async Task<bool> Read() {
    await ValueTask.CompletedTask;
    return false;
  }

  public async Task<Candle> LoadOne() {
    await ValueTask.CompletedTask;
    throw new Exception("not using");
  }
}
