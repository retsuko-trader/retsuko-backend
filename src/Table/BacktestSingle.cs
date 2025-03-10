public record struct BacktestSingle(
  string id,
  string run_id,
  string dataset,
  DateTime dataset_start,
  DateTime dataset_end,
  string strategy_name,
  string strategy_config,
  string broker_config,
  string metrics
) {
  public static string TableName => "backtest_single";
}
