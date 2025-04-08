namespace Retsuko.Core;

public ref struct MetricsHelper {
  double initialBalance;
  ref Portfolio portfolio;
  ref TraderMetrics metrics;
  ref List<Trade> trades;
  Candle firstCandle;
  Candle lastCandle;

  int days;

  public MetricsHelper(
    double initialBalance,
    ref Portfolio portfolio,
    ref TraderMetrics metrics,
    ref List<Trade> trades,
    Candle firstCandle,
    Candle lastCandle
  ) {
    this.initialBalance = initialBalance;
    this.portfolio = ref portfolio;
    this.metrics = ref metrics;
    this.trades = ref trades;
    this.firstCandle = firstCandle;
    this.lastCandle = lastCandle;

    days = Math.Max(1, (lastCandle.ts - firstCandle.ts).Days);
  }

  public double sortino() {
    var profit = metrics.totalProfit;
    var expectedReturn = profit / days;

    var down = 0.0;
    foreach (var trade in trades) {
      if (trade.profit > 0) {
        continue;
      }
      down += Math.Pow(trade.profit - expectedReturn, 2);
    }

    var downStdev = Math.Sqrt(down / trades.Count);
    return expectedReturn / downStdev * Math.Sqrt(365);
  }

  public double sharpe() {
    var profit = metrics.totalProfit;
    var expectedReturn = profit / days;

    var sum = 0.0;
    foreach (var trade in trades) {
      sum += Math.Pow(trade.profit - expectedReturn, 2);
    }

    var stdev = Math.Sqrt(sum / trades.Count);
    return expectedReturn / stdev * Math.Sqrt(365);
  }

  public double cagr() {
    return Math.Pow(portfolio.totalBalance / initialBalance, 1 / (days / 365.0)) - 1;
  }

  public double avgTrades() {
    return (double)metrics.totalTrades / days;
  }
}
