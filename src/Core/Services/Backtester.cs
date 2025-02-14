public class Backtester {
  private readonly BacktestConfig config;
  private BacktestMetrics metrics;

  public Backtester(BacktestConfig config) {
    this.config = config;
    this.metrics = BacktestMetrics.Empty;
  }
}
