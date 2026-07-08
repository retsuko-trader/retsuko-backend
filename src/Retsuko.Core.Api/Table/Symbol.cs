using DuckDB.NET.Data;

namespace Retsuko;

public record struct Symbol(
  int id,
  string name
) {
  public static async Task<Symbol[]> List() {
    using var command = Database.Candle.CreateCommand();
    command.CommandText = "SELECT id, name FROM symbol ORDER BY id asc";

    var reader = await command.ExecuteReaderAsync();
    var result = new List<Symbol>();
    while (reader.Read()) {
      var id = reader.GetInt32(0);
      var name = reader.GetString(1);

      var symbol = new Symbol(id, name);
      result.Add(symbol);
    }

    return result.ToArray();
  }

  public static async Task<Symbol?> Get(int id) {
    using var command = Database.Candle.CreateCommand();
    command.CommandText = "SELECT name FROM symbol WHERE id = $id";
    command.Parameters.Add(new DuckDBParameter("id", id));

    var reader = await command.ExecuteReaderAsync();
    if (!reader.Read()) {
      return null;
    }

    return new Symbol(id, reader.GetString(0));
  }

  public static async Task<Symbol?> Get(string name) {
    using var command = Database.Candle.CreateCommand();
    command.CommandText = "SELECT id FROM symbol WHERE name = $name";
    command.Parameters.Add(new DuckDBParameter("name", name));

    var reader = await command.ExecuteReaderAsync();
    if (!reader.Read()) {
      return null;
    }

    return new Symbol(reader.GetInt32(0), name);
  }
}
