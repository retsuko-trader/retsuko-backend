public record struct DatasetConfig(
  Market market,
  string symbol,
  int interval,
  DateTime start,
  DateTime end
) {
}
