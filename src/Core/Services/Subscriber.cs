using System.Text.Json;
using Binance.Net.Enums;
using Retsuko.Core.Events;
using Retsuko.Plugins;
using StackExchange.Redis;
using Kline = Binance.Net.Objects.Models.Spot.Socket.BinanceStreamKline;

namespace Retsuko.Core;

public static class Subscriber {
  private static IDatabase db;
  private static SemaphoreSlim processMutex = new(1, 1);

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
    await ProcessQueue(CallbackContextKind.Subscription);
  }

  public static async Task HandleCallbackManual() {
    await ProcessQueue(CallbackContextKind.Manual);
  }

  public static async Task ProcessQueue(CallbackContextKind contextKind) {
    const string queueName = "worker:queue";

    await processMutex.WaitAsync();

    try {
      var list = await db.ListRangeAsync(queueName, -1, -1);

      while (list != null && list.Length > 0 && !list[0].IsNull) {
        var pop = list[0];
        var data = JsonSerializer.Deserialize<QueueData>(pop.ToString())!;

        MyLogger.Logger.LogInformation("Processing candle from subscriber; {id} {symbol}:{interval} {kline}", data.id, data.symbol, data.interval, data.kline);

        var (result, exception) = await Handle(data);

        if (!result) {
          MyLogger.Logger.LogError("failed to handle candles from subscriber; {id} {symbol} {kline}", data.id, data.symbol, data.kline);
          EventDispatcher.Event(new CallbackFailEvent(
            id: data.id,
            symbol: data.symbol,
            interval: data.interval,
            kline: data.kline,
            queueLength: (int)await db.ListLengthAsync(queueName),
            contextKind: contextKind,
            exception: exception
          ));

          break;
        }

        await db.ListRightPopAsync(queueName);
        list = await db.ListRangeAsync(queueName, -1, -1);
      }
    } finally {
      processMutex.Release();
    }
  }

  private static async Task<(bool, Exception?)> Handle(QueueData data) {
    var symbol = await Symbol.Get(data.symbol);

    if (!symbol.HasValue) {
      return (false, new Exception($"symbol {data.symbol} not found"));
    }

    try {
      var candle = Candle.From(Market.futures, symbol.Value.id, data.interval, data.kline);
      await LiveCandleDispatcher.Dispatch(data.id, symbol.Value, data.interval, candle);
    } catch (Exception e) {
      MyLogger.Logger.LogError(e, "failed to handle candles from subscriber; {id} {symbol} {kline}", data.id, data.symbol, data.kline);
      return (false, e);
    }

    return (true, null);
  }

  record QueueData(string id, string symbol, KlineInterval interval, Kline kline);
}
