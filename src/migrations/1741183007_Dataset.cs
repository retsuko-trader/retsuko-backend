namespace Retsuko.Migrations;

public static partial class Migrations {
  public static async Task MigrateDataset() {
    using var command = Database.Candle.CreateCommand();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS dataset (
      market INT,
      symbolId INT,
      interval INT,
      start TIMESTAMP,
      ""end"" TIMESTAMP,
      count INT,
      PRIMARY KEY (market, symbolId, interval)
    )";
    await command.ExecuteNonQueryAsync();
  }
}
