public record struct BacktestReport(
  BacktestConfig config,
  double startBalance,
  double endBalance,
  double profit,
  Trade[] trades,
  BacktestMetrics metrics
) {
}

public record struct BacktestMetrics(
  int totalTrades,
  int avgTrades,
  double totalProfit,
  double cagr,
  double sortino,
  double sharpe,
  double calmar,
  double minBalance,
  DateTime minBalanceTs,
  double maxBalance,
  DateTime maxBalanceTs,
  double drawdown,
  double drawdownHigh,
  double drawdownLow,
  DateTime drawdownStartTs,
  DateTime drawdownEndTs,
  double marketChange
) {
  public static BacktestMetrics Empty => new(
    0, 0, 0, 0, 0, 0, 0, 0, DateTime.MinValue, 0, DateTime.MinValue, 0, 0, 0, DateTime.MinValue, DateTime.MinValue, 0
  );
}
