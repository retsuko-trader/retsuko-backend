using System.Text.Json;
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

  public static async Task SubscriptionHandler() {
    var pop = await db.ListRightPopAsync("worker:queue");

    while (pop != RedisValue.Null) {
      var data = JsonSerializer.Deserialize<QueueData>(pop.ToString())!;

      // MyLogger.Logger.LogInformation("fetched {id} {interval} candles", data.id, data.kline);

      pop = await db.ListRightPopAsync("worker:queue");
    }
  }

  record QueueData(string id, Kline kline);
}