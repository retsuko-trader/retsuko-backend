using System.Text.Json;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Retsuko.Core;

namespace Retsuko;

public record struct PaperTraderState(
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
  public static string TableName => "paper_trader";

  public readonly PapertraderConfig Config => new(
    info: new(name, description),
    dataset: JsonSerializer.Deserialize<PapertraderDatasetConfig>(dataset)!,
    strategy: new(strategy_name, strategy_config),
    broker: JsonSerializer.Deserialize<PaperBrokerConfig>(broker_config)!
  );

  public readonly TraderMetrics Metrics => JsonSerializer.Deserialize<TraderMetrics>(metrics)!;

  public static PaperTraderState From(System.Data.Common.DbDataReader reader) {
    return new PaperTraderState(
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

  public static async Task<IReadOnlyList<PaperTraderState>> List() {
    using var command = Database.PaperTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} ORDER BY created_at DESC";

    using var reader = await command.ExecuteReaderAsync();
    var traders = new List<PaperTraderState>();

    while (await reader.ReadAsync()) {
      traders.Add(From(reader));
    }

    return traders;
  }

  public static async Task<PaperTraderState?> Get(string id) {
    using var command = Database.PaperTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE id = $id";
    command.Parameters.Add(new DuckDBParameter("id", id));

    using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) {
      return null;
    }

    return From(reader);
  }

  public void Insert() {
    using var appender = Database.PaperTrader.CreateAppender(TableName);
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
      .AppendValue(states);

    appender.Close();
  }

  public async Task Update() {
    using var command = Database.PaperTrader.CreateCommand();
    command.CommandText = $@"
      UPDATE {TableName}
      SET
        name = $name,
        description = $description,
        updated_at = $updated_at,
        ended_at = $ended_at,
        strategy_state = $strategy_state,
        broker_state = $broker_state,
        metrics = $metrics,
        states = $states
      WHERE id = $id
    ";
    command.Parameters.Add(new DuckDBParameter("id", id));
    command.Parameters.Add(new DuckDBParameter("name", name));
    command.Parameters.Add(new DuckDBParameter("description", description));
    command.Parameters.Add(new DuckDBParameter("updated_at", updatedAt));
    command.Parameters.Add(new DuckDBParameter("ended_at", endedAt));
    command.Parameters.Add(new DuckDBParameter("strategy_state", strategy_state));
    command.Parameters.Add(new DuckDBParameter("broker_state", broker_state));
    command.Parameters.Add(new DuckDBParameter("metrics", metrics));
    command.Parameters.Add(new DuckDBParameter("states", states));

    await command.ExecuteNonQueryAsync();
  }
}
