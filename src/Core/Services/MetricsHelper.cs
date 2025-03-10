namespace Retsuko.Core;

public ref struct MetricsHelper {
  double initialBalance;
  ref Portfolio portfolio;
  ref TraderMetrics metrics;
  Candle firstCandle;
  Candle lastCandle;

  int days;

  public MetricsHelper(
    double initialBalance,
    ref Portfolio portfolio,
    ref TraderMetrics metrics,
    Candle firstCandle,
    Candle lastCandle
  ) {
    this.initialBalance = initialBalance;
    this.portfolio = ref portfolio;
    this.metrics = ref metrics;
    this.firstCandle = firstCandle;
    this.lastCandle = lastCandle;

    days = Math.Max(1, (lastCandle.ts - firstCandle.ts).Days);
  }

  public double cagr() {
    return Math.Pow(portfolio.totalBalance / initialBalance, 1 / (days / 365.0)) - 1;
  }

  public double avgTrades() {
    return (double)metrics.totalTrades / days;
  }
}
