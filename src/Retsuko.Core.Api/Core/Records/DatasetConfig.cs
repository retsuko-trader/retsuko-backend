using Binance.Net.Enums;

namespace Retsuko.Core;

public record struct DatasetConfig(
  Market market,
  int symbolId,
  KlineInterval interval,
  DateTime start,
  DateTime end
) {
}
