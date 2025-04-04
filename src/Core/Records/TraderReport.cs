namespace Retsuko.Core;

public record struct TraderReport(
  BacktestConfig config,
  IEnumerable<Trade> trades,
  TraderMetrics metrics,
  IEnumerable<ExtDebugIndicator> debugIndicators
);

public record struct TraderMetrics(
  double startBalance,
  double endBalance,
  double asset,
  double currency,
  double totalBalance,
  int totalTrades,
  double avgTrades,
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
  public static TraderMetrics Empty => new(
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, double.MaxValue, DateTime.MinValue, 0, DateTime.MinValue, 0, 0, 0, DateTime.MinValue, DateTime.MinValue, 0
  );
}
