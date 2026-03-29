using System.Text.Json;
using DuckDB.NET.Data;
using Retsuko.Core;

namespace Retsuko;

public record struct LiveTraderHistory(
  string id,
  string traderId,
  DateTime ts,
  string kind,
  string prevState,
  string newState,
  string message
) {
  public const string TableName = "live_trader_history";

  public static LiveTraderHistory From(System.Data.Common.DbDataReader reader) {
    return new LiveTraderHistory(
      id: reader.GetString(0),
      traderId: reader.GetString(1),
      ts: reader.GetDateTime(2),
      kind: reader.GetString(3),
      prevState: reader.GetString(4),
      newState: reader.GetString(5),
      message: reader.GetString(6)
    );
  }

  public static async Task<IReadOnlyList<LiveTraderHistory>> List() {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} ORDER BY ts DESC";

    using var reader = await command.ExecuteReaderAsync();
    var histories = new List<LiveTraderHistory>();

    while (reader.Read()) {
      var trader = From(reader);
      histories.Add(trader);
    }

    return histories;
  }

  public static async Task<IReadOnlyList<LiveTraderHistory>> List(string traderId) {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE traderId = $traderId ORDER BY ts DESC";
    command.Parameters.Add(new DuckDBParameter("traderId", traderId));

    using var reader = await command.ExecuteReaderAsync();
    var histories = new List<LiveTraderHistory>();

    while (reader.Read()) {
      var trader = From(reader);
      histories.Add(trader);
    }

    return histories;
  }

  public void Insert() {
    using var appender = Database.LiveTrader.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(id)
      .AppendValue(traderId)
      .AppendValue(ts)
      .AppendValue(kind)
      .AppendValue(prevState)
      .AppendValue(newState)
      .AppendValue(message)
      .EndRow();

    appender.Close();
  }

  public static LiveTraderHistory Create(string traderId, LiveTraderState prev, LiveTraderState next, Message entry) {
    var prevState = JsonSerializer.Serialize(prev);
    var nextState = JsonSerializer.Serialize(next);
    var content = JsonSerializer.Serialize(entry);

    return new LiveTraderHistory(
      id: new Visus.Cuid.Cuid2().ToString(),
      traderId: traderId,
      ts: DateTime.UtcNow,
      kind: entry.Kind,
      prevState: prevState,
      newState: nextState,
      message: content
    );
  }

  public abstract record Message {
    public abstract string Kind { get; }
  }

  public record MessageTick(
    Candle candle,
    Trade? trade,
    bool force
  ): Message {
    public override string Kind => "update";
  }

  public record MessageMigrate(
    string config
  ): Message {
    public override string Kind => "migrate";
  }
}
