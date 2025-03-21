using System.Text.Json;
using Binance.Net.Enums;
using StackExchange.Redis;
using Kline = Binance.Net.Objects.Models.Spot.Socket.BinanceStreamKline;

namespace Retsuko.Core;

public static class Subscriber {
  private static IDatabase db;

  static Subscriber() {
    var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions {
      Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
      EndPoints = { $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}" },
    });

    db = redis.GetDatabase();
  }

  public static async Task Subscribe(string id, string symbol, KlineInterval interval) {
    var workerUrl = Environment.GetEnvironmentVariable("WORKER_URL")!;
    var url = $"{workerUrl}/subscribe";

    var client = new HttpClient();
    var resp = await client.PostAsJsonAsync(url, new { id, symbol, interval });

    resp.EnsureSuccessStatusCode();
  }

  public static async Task UnSubscribe(string id) {
    var workerUrl = Environment.GetEnvironmentVariable("WORKER_URL")!;
    var url = $"{workerUrl}/subscribe/{id}";

    var client = new HttpClient();
    var resp = await client.DeleteAsync(url);

    resp.EnsureSuccessStatusCode();
  }

  public static async Task HandleCallbackFromWorker() {
    var pop = await db.ListRightPopAsync("worker:queue");

    while (pop != RedisValue.Null) {
      var data = JsonSerializer.Deserialize<QueueData>(pop.ToString())!;
      var result = await Handle(data);

      if (!result) {
        MyLogger.Logger.LogError("failed to handle candles from subscriber; {id} {symbol} {kline}", data.id, data.symbol, data.kline);
      }

      pop = await db.ListRightPopAsync("worker:queue");
    }
  }

  private static async Task<bool> Handle(QueueData data) {
    var symbol = await Symbol.Get(data.symbol);

    if (!symbol.HasValue) {
      return false;
    }

    var candle = Candle.From(Market.futures, symbol.Value.id, data.interval, data.kline);
    await LiveCandleDispatcher.Dispatch(data.id, symbol.Value, data.interval, candle);

    return true;
  }

  record QueueData(string id, string symbol, KlineInterval interval, Kline kline);
}
