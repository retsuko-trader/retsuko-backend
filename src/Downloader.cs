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
    var binance = new BinanceRestClient();
    var api = binance.UsdFuturesApi.ExchangeData;
    var exchanges = await api.GetExchangeInfoAsync();
    var symbols = exchanges.Data.Symbols.ToArray();

    foreach (var symbol in symbols) {
      foreach (var interval in intervals) {
        try {
          MyLogger.Logger.LogInformation("Start downloading {symbol} {interval} candles", symbol.Name, interval);

          await DownloadCandles(symbol.Name, interval, null);
        } catch (Exception ex) {
          MyLogger.Logger.LogError(ex, "Error downloading {symbol} {interval}", symbol.Name, interval);
        }
      }
    }

    MyLogger.Logger.LogInformation("Download complete");
  }

  public static async Task UpdateAll() {
    var tracer = MyTracer.Tracer;
    using var datasetSpan = tracer.StartActiveSpan("Downloader.GetDataset");
    var datasets = await Candle.GetDataset();
    datasetSpan.End();


    using var span = tracer.StartActiveSpan("Downloader.DownloadCandles");
    await Parallel.ForEachAsync(datasets, new ParallelOptions { MaxDegreeOfParallelism = 2 }, async (dataset, t) => {
      var attributes = new OpenTelemetry.Trace.SpanAttributes(new Dictionary<string, object?> {
        { "symbol", dataset.symbol },
        { "interval", dataset.interval },
        { "end", dataset.end }
      });
      using var ev = span.AddEvent("DownloadCandles", default, attributes);
      await DownloadCandles(dataset.symbol, (KlineInterval)dataset.interval, dataset.end);
      ev.End();
    });
    span.End();
  }

  static async Task DownloadCandles(string symbol, KlineInterval interval, DateTime? start) {
    var origStart = start;

    void Insert(Market market, string symbol, KlineInterval interval, IEnumerable<Binance.Net.Interfaces.IBinanceKline> candles) {
      using var appender = Database.Candle.CreateAppender(Candle.TableName);
      foreach (var kline in candles) {
        try {
          var candle = Candle.From(market, symbol, interval, kline);
          var row = appender.CreateRow();
          candle.AppendRow(row);
        } catch (Exception ex) {
          MyLogger.Logger.LogError(ex, "Error inserting {symbol} {interval} candle", symbol, interval);
        }
      }
    }

    async Task<DateTime?> GetStart() {
      if (start.HasValue) {
        var candles = await Broker.API.ExchangeData.GetKlinesAsync(symbol, interval, start, null, 2);
        if (candles.Data.Count() <= 1) {
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
    var candles = await api.GetKlinesAsync(symbol, interval, start, null, 1000);

    if (!candles.Data.Any()) {
      return;
    }

    Insert(Market.futures, symbol, interval, candles.Data);

    var end = candles.Data.Last().OpenTime;

    while (candles.Data.Count() >= 1000) {
      candles = await api.GetKlinesAsync(symbol, interval, end, null, 1000);

      if (!candles.Data.Any()) {
        break;
      }

      end = candles.Data.Last().OpenTime;
      Insert(Market.futures, symbol, interval, candles.Data.Skip(1));
    }
  }
}
