public record struct BacktestRun(
  string id,
  string name,
  string description,
  DateTime created_at,
  DateTime ended_at,
  string datasets,
  string strategies,
  string broker_config
) {
  public static string TableName => "backtest_run";
}
