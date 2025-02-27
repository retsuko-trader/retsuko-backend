using Binance.Net.Enums;

public record struct DatasetConfig(
  Market market,
  string symbol,
  KlineInterval interval,
  DateTime start,
  DateTime end
) {
}
