using Binance.Net.Clients;
using Binance.Net.Enums;
using Retsuko;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var binance = new BinanceRestClient();
var api = binance.UsdFuturesApi.ExchangeData;
var exchanges = await api.GetExchangeInfoAsync();
var symbols = exchanges.Data.Symbols.ToArray();

var intervals = new [] {
  KlineInterval.OneMinute,
  KlineInterval.ThreeMinutes,
  KlineInterval.FiveMinutes,
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
};

var command = Database.Candle.CreateCommand();
command.CommandText = "CREATE TABLE IF NOT EXISTS candle (market TEXT, symbol TEXT, interval INT, ts TIMESTAMP, open DOUBLE, high DOUBLE, low DOUBLE, close DOUBLE, volume DOUBLE, PRIMARY KEY (market, symbol, interval, ts))";
command.ExecuteNonQuery();

command.CommandText = "CREATE INDEX IF NOT EXISTS candle_symbol_index ON candle (market, symbol, interval)";
command.ExecuteNonQuery();

void Insert(Market market, string symbol, int interval, IEnumerable<Binance.Net.Interfaces.IBinanceKline> candles) {
  using var appender = Database.Candle.CreateAppender("candle");

  foreach (var kline in candles) {
    var candle = Candle.From(market, symbol, kline);
    var row = appender.CreateRow();
    candle.AppendRow(row);
  }
}

foreach (var symbol in symbols) {
  foreach (var interval in intervals) {
    try {
    Console.WriteLine($"Start downloading {symbol.Name} {interval} candles");

    var candles = await api.GetKlinesAsync(symbol.Name, interval, null, null, 1000);
    var count = candles.Data.Count();

    if (count == 0) {
      Console.WriteLine($"No candles for {symbol.Name} {interval}");
      continue;
    }
      var start = candles.Data.Last().CloseTime;
      var end = candles.Data.First().CloseTime;

      Insert(Market.futures, symbol.Name, (int)interval, candles.Data);

      while (candles.Data.Count() >= 1000) {
        candles = await api.GetKlinesAsync(symbol.Name, interval, null, end, 1000);

        if (candles.Data.Count() == 0) {
          break;
        }

        count += candles.Data.Count();
        end = candles.Data.First().CloseTime;
        Insert(Market.futures, symbol.Name, (int)interval, candles.Data.Take(candles.Data.Count() - 1));
      }

      Console.WriteLine($"Downloaded {symbol.Name} {interval} candles: {count} from {start} to {end}");

    } catch (Exception ex) {
      Console.WriteLine($"Error downloading {symbol.Name} {interval} candles: {ex.Message}");
    }
  }
}

// app.Run();
