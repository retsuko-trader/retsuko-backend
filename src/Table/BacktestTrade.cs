public record struct BacktestTrade(
  string single_id,
  DateTime ts,
  string action,
  double confidence,
  double asset,
  double currency,
  double price,
  double profit
) {
  public static string TableName => "backtest_trade";
}
