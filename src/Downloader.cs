using Binance.Net.Clients;
using Binance.Net.Enums;
using Retsuko;
using Retsuko.Core;

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

  public static async Task DownloadAll() {
    var symbols = await Symbol.List();

    foreach (var symbol in symbols) {
      foreach (var interval in intervals) {
        try {
          MyLogger.Logger.LogInformation("Start downloading {symbol} {interval} candles", symbol.name, interval);

          await DownloadCandles(symbol.id, interval, null);
        } catch (Exception ex) {
          MyLogger.Logger.LogError(ex, "Error downloading {symbol.name} {interval}", symbol.name, interval);
        }
      }
    }

    MyLogger.Logger.LogInformation("Download complete");
  }

  public static async Task UpdateAll() {
    var tracer = MyTracer.Tracer;
    using var datasetSpan = tracer.StartActiveSpan("Downloader.GetDataset");
    var datasets = await Dataset.List();
    datasetSpan.End();

    using var span = tracer.StartActiveSpan("Downloader.DownloadCandles");
    await Parallel.ForEachAsync(datasets, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (dataset, t) => {
      var attributes = new OpenTelemetry.Trace.SpanAttributes(new Dictionary<string, object?> {
        { "symbolId", dataset.symbolId },
        { "interval", dataset.interval },
        { "end", dataset.end }
      });
      using var ev = span.AddEvent("DownloadCandles", default, attributes);
      await DownloadCandles(dataset.symbolId, (KlineInterval)dataset.interval, dataset.end);
      ev.End();
    });
    span.End();
  }

  static async Task DownloadCandles(int symbolId, KlineInterval interval, DateTime? start) {
    var symbol = await Symbol.Get(symbolId);
    if (symbol == null) {
      MyLogger.Logger.LogError("Symbol {symbolId} not found", symbolId);
      return;
    }
    var symbolName = symbol.Value.name;

    using var db = Database.CreateCandleDatabase(symbol.Value.name);
    var origStart = start;

    void Insert(Market market, int symbolId, KlineInterval interval, IEnumerable<Binance.Net.Interfaces.IBinanceKline> candles) {
      try {
        using var appender = db.CreateAppender(Candle.TableName);
        foreach (var kline in candles) {
            var candle = Candle.From(market, symbolId, interval, kline);
            var row = appender.CreateRow();
            candle.AppendRow(row);
        }
      } catch (Exception ex) {
        MyLogger.Logger.LogError(ex, "Error inserting {symbolId}({symbolName}) {interval} candle", symbolId, symbolName, interval);
      }
    }

    async Task<DateTime?> GetStart() {
      if (start.HasValue) {
        var candles = await Broker.API.ExchangeData.GetKlinesAsync(symbolName, interval, start, null, 2);
        if (candles.Data == null || candles.Data.Count() <= 1) {
          return null;
        }
        return candles.Data.Skip(1).First().OpenTime;
      } else {
        return DateTime.Parse("2000-01-01 00:00:00");
      }
    }

    start = await GetStart();
    MyLogger.Logger.LogInformation("Start downloading {symbol} {interval}: {origStart} => {start}", symbol, interval, origStart, start);
    if (!start.HasValue) {
      return;
    }

    var api = Broker.API.ExchangeData;
    var candles = await api.GetKlinesAsync(symbolName, interval, start, null, 1000);

    if (!candles.Data.Any()) {
      return;
    }

    Insert(Market.futures, symbolId, interval, candles.Data);

    var end = candles.Data.Last().OpenTime;

    while (candles.Data.Count() >= 1000) {
      candles = await api.GetKlinesAsync(symbolName, interval, end, null, 1000);

      if (!candles.Data.Any()) {
        break;
      }

      end = candles.Data.Last().OpenTime;
      Insert(Market.futures, symbolId, interval, candles.Data.Skip(1));
    }

    var dataset = await Dataset.GetFrom(db, interval);
    try {
      await Dataset.Upsert(dataset);
    } catch (Exception ex) {
      MyLogger.Logger.LogError(ex, "Error upserting {symbolId}({symbolName}) {interval} dataset: {dataset}", symbolId, symbolName, interval, dataset);
    }
  }
}
