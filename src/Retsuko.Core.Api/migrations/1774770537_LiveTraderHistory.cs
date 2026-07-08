namespace Retsuko.Migrations;

public static partial class Migrations {
  public static async Task CreateLiveTraderHistory() {
    using var command = Database.Candle.CreateCommand();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS live_trader_history (
      id STRING,
      traderId STRING,
      ts TIMESTAMP,
      kind STRING,
      content STRING,
      PRIMARY KEY (id)
    )";

    await command.ExecuteNonQueryAsync();
  }
}
