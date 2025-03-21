namespace Retsuko;

public record struct PaperTrader(
  string id,
  string name,
  string description,
  DateTime created_at,
  DateTime updated_at,
  DateTime? ended_at,
  int symbolId,
  int interval,
  string strategy_name,
  string strategy_config,
  string strategy_state,
  string broker_state,
  string metrics
) {
  public static string TableName => "paper_trader";
}
