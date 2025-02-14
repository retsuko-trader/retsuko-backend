public record struct PaperBrokerConfig(
  double initialBalance,
  double fee,
  bool enableMargin,
  bool validTradeOnly
) {
}
