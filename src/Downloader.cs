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

    void Insert(Market market, string symbol, KlineInterval interval, IEnumerable<Binance.Net.Interfaces.IBinanceKline> candles) {
      using var appender = Database.Candle.CreateAppender("candle");

      foreach (var kline in candles) {
        var candle = Candle.From(market, symbol, interval, kline);
        var row = appender.CreateRow();
        candle.AppendRow(row);
      }
    }

    foreach (var symbol in symbols) {
      foreach (var interval in intervals) {
        try {
          Console.WriteLine($"Start downloading {symbol.Name} {interval} candles");

          await DownloadCandles(symbol.Name, interval, null);
        } catch (Exception ex) {
          Console.WriteLine($"Error downloading {symbol.Name} {interval} candles: {ex.Message}");
        }
      }
    }
  }

  public static async Task UpdateAll() {
    var tracer = MyTracer.Tracer;
    using var rootSpan = tracer.StartRootSpan("Downloader.UpdateAll");

    using var datasetSpan = tracer.StartActiveSpan("Downloader.GetDataset");
    var datasets = await Candle.GetDataset();
    datasetSpan.End();

    foreach (var dataset in datasets) {
      using var span = tracer.StartActiveSpan("Downloader.DownloadCandles");
      span.SetAttribute("symbol", dataset.symbol);
      span.SetAttribute("interval", dataset.interval.ToString());
      span.SetAttribute("start", dataset.end.ToString());

      await DownloadCandles(dataset.symbol, (KlineInterval)dataset.interval, dataset.end);

      span.End();
    }

    rootSpan.End();
  }

  static async Task DownloadCandles(string symbol, KlineInterval interval, DateTime? start) {
    using var appender = Database.Candle.CreateAppender(Candle.TableName);
    void Insert(Market market, string symbol, KlineInterval interval, IEnumerable<Binance.Net.Interfaces.IBinanceKline> candles) {
      foreach (var kline in candles) {
        var candle = Candle.From(market, symbol, interval, kline);
        var row = appender.CreateRow();
        candle.AppendRow(row);
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
        var candles = await Broker.API.ExchangeData.GetKlinesAsync(symbol, interval, null, null, 1);
        if (!candles.Data.Any()) {
          return null;
        }
        return candles.Data.First().OpenTime;
      }
    }

    start = await GetStart();
    if (!start.HasValue) {
      return;
    }

    var api = Broker.API.ExchangeData;
    var candles = await api.GetKlinesAsync(symbol, interval, start, null, 1000);

    if (!candles.Data.Any()) {
      return;
    }

    Insert(Market.futures, symbol, interval, candles.Data);

    var end = candles.Data.First().OpenTime;

    while (candles.Data.Count() >= 1000) {
      candles = await api.GetKlinesAsync(symbol, interval, null, end, 1000);

      if (!candles.Data.Any()) {
        break;
      }

      end = candles.Data.First().OpenTime;
      Insert(Market.futures, symbol, interval, candles.Data.Take(candles.Data.Count() - 1));
    }
  }
}
