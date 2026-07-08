using System.Text.Json;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Retsuko.Diagnostics;
using StackExchange.Redis;

// BinanceStreamKlineData
using Kline = Binance.Net.Objects.Models.Spot.Socket.BinanceStreamKline;

public class Subscriber {
  record Subscription(string symbol, KlineInterval interval, Kline? lastKline, CancellationTokenSource cts);

  record SubscriptionState(string symbol, KlineInterval interval) {}

  private readonly IDatabase db;
  private readonly BinanceSocketClient client = new();
  private readonly BinanceRestClient restClient = new();
  private readonly Dictionary<string, Subscription> subscriptions = [];

  public Subscriber() {
    var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions {
      Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
      EndPoints = { $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}" },
    });

    db = redis.GetDatabase();
  }

  public async Task Init() {
    await LoadSubscription();
  }

  public async Task Subscribe(string id, string symbol, KlineInterval interval) {
    var cts = new CancellationTokenSource();
    var subscription = new Subscription(symbol, interval, null, cts);

    await SubscribeInner(id, subscription);

    await db.HashSetAsync("worker:store", id, JsonSerializer.Serialize(new SubscriptionState(symbol, interval)));
  }

  private async Task LoadSubscription() {
    var entries = await db.HashGetAllAsync("worker:store");

    foreach (var entry in entries) {
      var id = entry.Name.ToString();
      var state = JsonSerializer.Deserialize<SubscriptionState>(entry.Value.ToString())!;
      var subscription = new Subscription(state.symbol, state.interval, null, new CancellationTokenSource());

      MyLogger.Logger.LogInformation("Loading subscription {id} for {symbol} with interval {interval}", id, state.symbol, state.interval);

      await SubscribeInner(id, subscription);
    }
  }

  private async Task SubscribeInner(string id, Subscription subscription) {
    if (subscriptions.ContainsKey(id)) {
      MyLogger.Logger.LogWarning("Subscription {id} already exists, skipping", id);
      return;
    }

    subscriptions[id] = subscription;
    await client.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(
      subscription.symbol,
      subscription.interval,
      async x => await OnData(id, (Kline)x.Data.Data),
      false,
      priceIndex: false,
      subscription.cts.Token
    );
  }

  public async Task Unsubscribe(string id) {
    if (subscriptions.TryGetValue(id, out var subscription)) {
      subscription.cts.Cancel();
      subscriptions.Remove(id);

      await db.HashDeleteAsync("worker:store", id);
    }
  }

  public async Task Reload(string id, int count) {
    if (!subscriptions.TryGetValue(id, out var subscription)) {
      MyLogger.Logger.LogWarning("Subscription {id} not found", id);
      return;
    }

    var klines = await restClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
      subscription.symbol,
      subscription.interval,
      limit: count
    );

    if (!klines.Success) {
      MyLogger.Logger.LogError("Failed to reload {id}: {error}", id, klines.Error);
    }

    foreach (var kline in klines.Data) {
      await OnData(id, new Kline {
        OpenTime = kline.OpenTime,
        CloseTime = kline.CloseTime,
        OpenPrice = kline.OpenPrice,
        ClosePrice = kline.ClosePrice,
        Volume = kline.Volume
      });
    }
  }

  private async Task OnData(string id, Kline kline) {
    if (!subscriptions.TryGetValue(id, out var subscription)) {
      return;
    }

    var next = subscriptions[id] with { lastKline = kline };
    if (subscription.lastKline == null) {
      subscriptions[id] = next;
      return;
    }

    var symbol = subscription.symbol;
    var interval = subscription.interval;

    if (subscription.lastKline.OpenTime < kline.OpenTime) {
      using var span = MyTracer.Tracer.StartRootSpan("OnKlineData");
      var k = subscription.lastKline;
      MyLogger.Logger.LogInformation("Processing kline for {id}: {interval} {openTime} {closeTime} {openPrice} {closePrice} {volume}", id, k.Interval, k.OpenTime, k.CloseTime, k.OpenPrice, k.ClosePrice, k.Volume);

      await db.ListLeftPushAsync("worker:queue", JsonSerializer.Serialize(new { id, symbol, interval, kline = k }));

      var client = new HttpClient();
      _ = Task.Run(() => client.PostAsJsonAsync(Const.CALLBACK_URL, new {}));
    }

    subscriptions[id] = next;
  }
}
