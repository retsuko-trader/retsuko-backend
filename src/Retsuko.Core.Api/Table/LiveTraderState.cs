using System.Text.Json;
using DuckDB.NET.Data;
using Retsuko.Core;

namespace Retsuko;

public record struct LiveTraderState(
  string id,
  string name,
  string description,
  DateTime createdAt,
  DateTime updatedAt,
  DateTime? endedAt,
  string dataset,
  string strategy_name,
  string strategy_config,
  string strategy_state,
  string broker_config,
  string broker_state,
  string metrics,
  string states
) {
  public const string TableName = "live_trader";

  public readonly LiveTraderConfig Config => new(
    info: new(name, description),
    dataset: JsonSerializer.Deserialize<LiveTraderDatasetConfig>(dataset)!,
    strategy: new(strategy_name, strategy_config),
    broker: JsonSerializer.Deserialize<LiveBrokerConfig>(broker_config)!
  );

  public readonly TraderMetrics Metrics => JsonSerializer.Deserialize<TraderMetrics>(metrics)!;

  public static LiveTraderState From(System.Data.Common.DbDataReader reader) {
    return new LiveTraderState(
      id: reader.GetString(0),
      name: reader.GetString(1),
      description: reader.GetString(2),
      createdAt: reader.GetDateTime(3),
      updatedAt: reader.GetDateTime(4),
      endedAt: reader.IsDBNull(5) ? null : reader.GetDateTime(5),
      dataset: reader.GetString(6),
      strategy_name: reader.GetString(7),
      strategy_config: reader.GetString(8),
      strategy_state: reader.GetString(9),
      broker_config: reader.GetString(10),
      broker_state: reader.GetString(11),
      metrics: reader.GetString(12),
      states: reader.GetString(13)
    );
  }

  public static async Task<IReadOnlyList<LiveTraderState>> List() {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} ORDER BY created_at DESC";

    using var reader = await command.ExecuteReaderAsync();
    var traders = new List<LiveTraderState>();

    while (reader.Read()) {
      var trader = From(reader);
      traders.Add(trader);
    }

    return traders;
  }

  public static async Task<LiveTraderState?> Get(string id) {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE id = $id";
    command.Parameters.Add(new DuckDBParameter("id", id));

    using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) {
      return null;
    }

    return From(reader);
  }

  public void Insert() {
    using var appender = Database.LiveTrader.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(name)
      .AppendValue(description)
      .AppendValue(createdAt)
      .AppendValue(updatedAt)
      .AppendValue(endedAt)
      .AppendValue(dataset)
      .AppendValue(strategy_name)
      .AppendValue(strategy_config)
      .AppendValue(strategy_state)
      .AppendValue(broker_config)
      .AppendValue(broker_state)
      .AppendValue(metrics)
      .AppendValue(states)
      .EndRow();

    appender.Close();
  }

  public async Task Update() {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $@"
      UPDATE {TableName} SET
        name = $name,
        description = $description,
        updated_at = $updatedAt,
        ended_at = $endedAt,
        dataset = $dataset,
        strategy_state = $strategy_state,
        broker_state = $broker_state,
        metrics = $metrics,
        states = $states
      WHERE id = $id
    ";

    command.Parameters.Add(new DuckDBParameter("id", id));
    command.Parameters.Add(new DuckDBParameter("name", name));
    command.Parameters.Add(new DuckDBParameter("description", description));
    command.Parameters.Add(new DuckDBParameter("updatedAt", updatedAt));
    command.Parameters.Add(new DuckDBParameter("endedAt", endedAt));
    command.Parameters.Add(new DuckDBParameter("dataset", dataset));
    command.Parameters.Add(new DuckDBParameter("strategy_state", strategy_state));
    command.Parameters.Add(new DuckDBParameter("broker_state", broker_state));
    command.Parameters.Add(new DuckDBParameter("metrics", metrics));
    command.Parameters.Add(new DuckDBParameter("states", states));

    await command.ExecuteNonQueryAsync();
  }
}
