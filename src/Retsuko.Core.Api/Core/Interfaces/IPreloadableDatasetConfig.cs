using Binance.Net.Enums;

namespace Retsuko.Core;

public interface IPreloadableDatasetConfig {
  public Market market { get; }
  public int symbolId { get; }
  public KlineInterval interval { get; }
  public int preloadCount { get; }
}
