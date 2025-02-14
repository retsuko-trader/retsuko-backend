public record struct Trade(
  DateTime ts,
  SignalKind signal,
  double confidence,
  double asset,
  double currency,
  double price,
  double profit
) {
}
