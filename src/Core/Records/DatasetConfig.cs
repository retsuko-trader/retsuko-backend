using Binance.Net.Enums;

namespace Retsuko.Core;

public record struct DatasetConfig(
  Market market,
  string symbol,
  KlineInterval interval,
  DateTime start,
  DateTime end
) {
}
