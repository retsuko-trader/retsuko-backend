using Binance.Net.Enums;
using DuckDB.NET.Data;

namespace Retsuko.Core;

public record KlineInput(
  Market market,
  int symbolId,
  KlineInterval interval
) {
  public void AddParameters(DuckDBCommand command) {
    command.Parameters.Add(new DuckDBParameter("market", (int)market));
    command.Parameters.Add(new DuckDBParameter("symbolId", symbolId));
    command.Parameters.Add(new DuckDBParameter("interval", (int)interval));
  }
}

public record KlineInfo(
  DateTime start,
  DateTime end,
  int count
);

public record KlineConsistencyResult(
  bool isSuccess,
  KlineInfo? info,
  DateTime? lastTsThreshold,
  List<DateTime> missingTimestamps,
  List<DateTime> duplicateTimestamps
);

public record KlineConsistencyResultEntry(
  KlineInput input,
  KlineConsistencyResult result
);

public static class CandleService {
  public static async Task<KlineInfo?> GetKlinesInfo(KlineInput input) {
    using var command = Database.Candle.CreateCommand();

    command.CommandText = "SELECT min(ts), max(ts), count(ts) FROM candle WHERE market = $market AND symbolId = $symbolId AND interval = $interval";
    input.AddParameters(command);

    var reader = await command.ExecuteReaderAsync();
    if (!reader.Read()) {
      return null;
    }

    var start = reader.GetDateTime(0);
    var end = reader.GetDateTime(1);
    var count = reader.GetInt32(2);

    return new KlineInfo(start, end, count);
  }

  public static async Task<KlineConsistencyResult> CheckConsistency(KlineInput input) {
    var info = await GetKlinesInfo(input);
    if (info == null) {
      return new KlineConsistencyResult(false, null, null, [], []);
    }

    using var duplicateCommand = Database.Candle.CreateCommand();
    duplicateCommand.CommandText = @"SELECT ts FROM candle WHERE market = $market AND symbolId = $symbolId AND interval = $interval GROUP BY ts HAVING count(ts) > 1";
    input.AddParameters(duplicateCommand);

    using var duplicateReader = await duplicateCommand.ExecuteReaderAsync();
    var duplicateTimestamps = new List<DateTime>();
    while (duplicateReader.Read()) {
      duplicateTimestamps.Add(duplicateReader.GetDateTime(0));
    }

    using var missingCommand = Database.Candle.CreateCommand();
    missingCommand.CommandText = @"SELECT x AS ts FROM
      (SELECT unnest(generate_series(min(ts), max(ts), $interval_ts)) FROM candle WHERE market = $market AND symbolId = $symbolId AND interval = $interval) tss(x)
      LEFT JOIN candle ON candle.ts = tss.x AND candle.market = $market AND candle.symbolId = $symbolId AND candle.interval = $interval
      WHERE candle.ts IS NULL";
    input.AddParameters(missingCommand);
    missingCommand.Parameters.Add(new DuckDBParameter("interval_ts", (int)input.interval));

    using var missingReader = await missingCommand.ExecuteReaderAsync();
    var missingTimestamps = new List<DateTime>();
    while (missingReader.Read()) {
      missingTimestamps.Add(missingReader.GetDateTime(0));
    }

    var lastTsThreshold = DateTime.UtcNow.Add(-TimeSpan.FromSeconds((int)input.interval)).Date.AddDays(-2);

    var isSuccess = missingTimestamps.Count == 0 && duplicateTimestamps.Count == 0 && info.end >= lastTsThreshold;
    return new KlineConsistencyResult(isSuccess, info, lastTsThreshold, missingTimestamps, duplicateTimestamps);
  }

  public static async Task<List<KlineConsistencyResultEntry>> CheckConsistencyForAll() {
    var datasets = await Dataset.List();

    var results = new List<KlineConsistencyResultEntry>();
    foreach (var dataset in datasets) {
      if (dataset.interval > (int)KlineInterval.OneDay) {
        continue;
      }

      var input = new KlineInput(dataset.market, dataset.symbolId, (KlineInterval)dataset.interval);
      var result = await CheckConsistency(input);
      results.Add(new(input, result));
    }

    return results;
  }
}
