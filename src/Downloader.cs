using System.Collections.Concurrent;
using System.IO.Compression;
using System.Web;
using System.Xml;
using Binance.Net.Clients;
using Binance.Net.Enums;
using DuckDB.NET.Data;
using Retsuko;
using Retsuko.Core;
using Retsuko.Migrations;

public static class Downloader {
  public static KlineInterval[] intervals = [
    KlineInterval.OneMinute,
    KlineInterval.ThreeMinutes,
    KlineInterval.FiveMinutes,
    KlineInterval.FifteenMinutes,
    KlineInterval.ThirtyMinutes,
    KlineInterval.OneHour,
    KlineInterval.TwoHour,
    KlineInterval.FourHour,
    KlineInterval.SixHour,
    KlineInterval.EightHour,
    KlineInterval.TwelveHour,
    KlineInterval.OneDay,
    KlineInterval.ThreeDay,
    KlineInterval.OneWeek,
    KlineInterval.OneMonth,
  ];

  public record KlineDownloadInfo(
    string symbol,
    KlineInterval interval,
    DateTime day,
    string url
  );

  public static async Task<IReadOnlyList<string>> GetKlines() {
    var url = "https://s3-ap-northeast-1.amazonaws.com/data.binance.vision?delimiter=/&prefix=data/futures/um/daily/klines/";

    var klineList = new List<string>();
    var iter = GetDataFromVision(url);
    await foreach (var prefix in iter) {
      var parts = prefix.Split('/');
      var symbol = parts[parts.Length - 2];

      klineList.Add(symbol);
    }

    return klineList;
  }

  public static async Task<IReadOnlyList<KlineInterval>> GetKlineIntervals(string symbol) {
    var url = $"https://s3-ap-northeast-1.amazonaws.com/data.binance.vision?delimiter=/&prefix=data/futures/um/daily/klines/{symbol}/";
    var intervalList = new List<KlineInterval>();

    var iter = GetDataFromVision(url);
    await foreach (var prefix in iter) {
      var parts = prefix.Split('/');
      var interval = parts[parts.Length - 2];

      if (interval == "") {
        continue;
      }

      intervalList.Add(interval.ToKlineInterval());
    }

    return intervalList;
  }

  public static async Task<IReadOnlyList<KlineDownloadInfo>> GetKlineArchives(string symbol, KlineInterval interval) {
    var url = $"https://s3-ap-northeast-1.amazonaws.com/data.binance.vision?delimiter=/&prefix=data/futures/um/daily/klines/{symbol}/{interval.ToIntervalString()}/";
    var result = new List<KlineDownloadInfo>();

    var iter = GetDataFromVision(url, null, "Contents", "Key");
    await foreach (var prefix in iter) {
      if (!prefix.EndsWith(".zip")) {
        continue;
      }

      var downloadUrl = $"https://data.binance.vision/{prefix}";
      var parts = prefix.Split('/');

      var filename = parts[parts.Length - 1];
      var parts2 = filename.Split('-');
      var year = int.Parse(parts2[2]);
      var month = int.Parse(parts2[3]);
      var day = int.Parse(parts2[4].Split('.')[0]);

      result.Add(new KlineDownloadInfo(symbol, interval, new DateTime(year, month, day), downloadUrl));
    }

    return result;
  }

  private static async IAsyncEnumerable<string> GetDataFromVision(string url, string? marker = null, string outer = "CommonPrefixes", string inner = "Prefix") {
    using var client = new HttpClient();
    using var resp = await client.GetAsync($"{url}&marker={HttpUtility.UrlEncode(marker)}");
    var xml = new XmlDocument();
    xml.Load(await resp.Content.ReadAsStreamAsync());

    var commonPrefixes = xml.GetElementsByTagName(outer);
    foreach (XmlNode commonPrefix in commonPrefixes) {
      var prefix = commonPrefix[inner]!.InnerText;

      yield return prefix;
    }

    var nextMarker = xml.GetElementsByTagName("NextMarker");
    if (nextMarker != null && nextMarker.Count > 0) {
      marker = nextMarker[0]!.InnerText;
      await foreach (var prefix in GetDataFromVision(url, marker, outer, inner)) {
        yield return prefix;
      }
    }
  }

  private static async Task DownloadArchive(DuckDBConnection db, KlineDownloadInfo archive, Symbol[] symbols, CancellationToken t) {
    using var client = new HttpClient();
    using var resp = await client.GetAsync(archive.url, t);
    using var zs = await resp.Content.ReadAsStreamAsync(t);
    using var zip = new ZipArchive(zs);

    var symbol = symbols.First(x => x.name == archive.symbol);

    using var appender = db.CreateAppender(Candle.TableName);
    foreach (var entry in zip.Entries) {
      using var fs = entry.Open();
      using var sr = new StreamReader(fs);

      while (!sr.EndOfStream) {
        var line = await sr.ReadLineAsync(t);
        if (line.StartsWith("open")) {
          continue;
        }

        var parts = line!.Split(',');

        var openTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(parts[0]));
        var open = double.Parse(parts[1]);
        var high = double.Parse(parts[2]);
        var low = double.Parse(parts[3]);
        var close = double.Parse(parts[4]);
        var volume = double.Parse(parts[5]);
        var closeTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(parts[6]));
        var quoteVolume = double.Parse(parts[7]);
        var count = int.Parse(parts[8]);
        var takerBuyVolume = double.Parse(parts[9]);
        var takerBuyQuoteVolume = double.Parse(parts[10]);

        var candle = new Candle(
          market: Market.futures,
          symbolId: symbol.id,
          interval: archive.interval,
          ts: openTime.DateTime,
          open: open,
          high: high,
          low: low,
          close: close,
          volume: volume
        );
        var row = appender.CreateRow();
        candle.AppendRow(row);
      }
    }
  }

  public static async Task DownloadAll() {
    var symbols = await Symbol.List();

    using var span = MyTracer.Tracer.StartActiveSpan("Downloader.DownloadCandles");
    var datasetChanges = new ConcurrentBag<Dataset>();

    await Parallel.ForEachAsync(symbols, new ParallelOptions { MaxDegreeOfParallelism = 64 }, async (symbol, t) => {
      var attributes = new OpenTelemetry.Trace.SpanAttributes(new Dictionary<string, object?> {
        { "symbolId", symbol.id },
        { "symbolName", symbol.name },
      });
      using var ev = span.AddEvent("DownloadSymbol", default, attributes);

      await DownloadSymbol(symbol.name, symbols, datasetChanges, t);
      ev.End();
    });

    foreach (var dataset in datasetChanges) {
      await Dataset.Upsert(dataset);
    }

    span.End();
  }

  private static async Task DownloadSymbol(string symbol, Symbol[] symbols, ConcurrentBag<Dataset> datasets, CancellationToken t) {
    MyLogger.Logger.LogTrace("Start downloading {symbol}", symbol);

    var intervals = await GetKlineIntervals(symbol);
    using var db = Database.CreateCandleDatabase(symbol);
    using (var command = db.CreateCommand()) {
      command.CommandText = @"CREATE TABLE IF NOT EXISTS candle (
        market INT,
        symbolId INT,
        interval INT,
        ts TIMESTAMP,
        open DOUBLE,
        high DOUBLE,
        low DOUBLE,
        close DOUBLE,
        volume DOUBLE,
        PRIMARY KEY (interval, ts)
      )";
      await command.ExecuteNonQueryAsync(t);
    }

    foreach (var interval in intervals) {
      var archives = (await GetKlineArchives(symbol, interval)).OrderBy(x => x.day).ToArray();
      MyLogger.Logger.LogTrace("Downloading {symbol} {interval}", symbol, interval);

      var dataset = await Dataset.GetFrom(db, interval);

      foreach (var archive in archives) {
        try {
          if (dataset.end > archive.day) {
            continue;
          }

          var exist = await Candle.GetFirstBetween(db, interval, archive.day, archive.day.AddDays(1));
          if (exist.HasValue) {
            continue;
          }

          // MyLogger.Logger.LogInformation("Start downloading {symbol} {interval} {day} {archive}", symbol, interval, archive.day, archive);
          await DownloadArchive(db, archive, symbols, t);
        } catch (Exception ex) {
          MyLogger.Logger.LogError(ex, "Error downloading {symbol} {interval} {archive}", symbol, interval, archive);
        }
      }

      dataset = await Dataset.GetFrom(db, interval);
      datasets.Add(dataset);
    }
  }
}
