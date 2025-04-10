using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Retsuko.Core.Events;

namespace Retsuko.Plugins;

public static class Discord {
  static DiscordSocketClient client;
  static SocketTextChannel channel;

  static Dictionary<long, SocketTextChannel> orderThreads = new();

  public static async Task Initialize() {
    client = new DiscordSocketClient();
    await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
    await client.StartAsync();

    client.Ready += async () => {
      await ValueTask.CompletedTask;

      var guild = client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("DISCORD_GUILD_ID")!));
      channel = guild.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID")!));
    };

    MyLogger.Logger.LogInformation("Discord bot started");

    EventDispatcher.OnLiveBrokerEvent += OnLiveBrokerEvent;
    EventDispatcher.OnException += OnException;
  }

  static async void OnLiveBrokerEvent(LiveBrokerEvent e) {
    if (e is LiveBrokerGotSignalEvent signalEvent) {
      await OnLiveBrokerGotSignalEvent(signalEvent);
    } else if (e is LiveBrokerOrderUpdateEvent orderUpdateEvent) {
      await OnLiveBrokerOrderUpdateEvent(orderUpdateEvent);
    } else if (e is LiveBrokerOrderFilledEvent orderFilledEvent) {
      await OnLiveBrokerOrderFilledEvent(orderFilledEvent);
    } else if (e is LiveBrokerOrderDelayedEvent orderDelayedEvent) {
      await OnLiveBrokerOrderDelayedEvent(orderDelayedEvent);
    }
  }

  static async Task OnLiveBrokerGotSignalEvent(LiveBrokerGotSignalEvent e) {
    var fields = new List<EmbedFieldBuilder> {
      new EmbedFieldBuilder()
        .WithName("TESTNET")
        .WithValue(e.trader.config.broker.isTestNet)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("trader.id")
        .WithValue(e.trader.Id)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("trader.name")
        .WithValue(e.trader.config.info.name)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("symbol")
        .WithValue(e.symbol)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("interval")
        .WithValue(e.trader.config.dataset.interval.ToIntervalString())
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("signal")
        .WithValue(e.signal.ToString())
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("orderId")
        .WithValue(e.order?.Data?.Id ?? -1)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("asset")
        .WithValue(e.assetInfo?.WalletBalance ?? -1)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("currency")
        .WithValue(e.currencyInfo?.WalletBalance ?? -1)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("totalBalance")
        .WithValue(e.accountInfo.TotalWalletBalance)
        .WithIsInline(true),
    };

    if (e.order == null) {
      await channel.SendMessageAsync(
        text: "LiveBroker order failed",
        embeds: [
          new EmbedBuilder()
            .WithTitle("LiveBroker order failed")
            .WithDescription($"Order failed for {e.trader.Id} on {e.symbol}")
            .WithColor(0xFF0000)
            .WithFields(fields)
            .Build()
        ]
      );
      return;
    }

    if (e.order.Error != null) {
      await channel.SendMessageAsync(
        text: "LiveBroker order error",
        embeds: [
          new EmbedBuilder()
            .WithTitle("LiveBroker order error")
            .WithDescription($"Order error for {e.trader.Id} on {e.symbol}\n{e.order.Error.Message}")
            .WithColor(0xFF0000)
            .WithFields(fields)
            .Build()
        ]
      );
      return;
    }

    fields.AddRange(
      new EmbedFieldBuilder()
        .WithName("order.side")
        .WithValue(e.order.Data.Side.ToString())
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.price")
        .WithValue(e.order.Data.Price)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.quantity")
        .WithValue(e.order.Data.Quantity)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.positionSide")
        .WithValue(e.order.Data.PositionSide.ToString())
        .WithIsInline(true)
    );

    var message = await channel.SendMessageAsync(
      text: "LiveBroker order created",
      embeds: [
        new EmbedBuilder()
          .WithTitle($"LiveBroker order created")
          .WithDescription($"Order created for {e.trader.Id} on {e.symbol}\nOrder Id={e.order.Data.Id}, side={e.order.Data.Side}, price={e.order.Data.Price}, quantity={e.order.Data.Quantity}")
          .WithColor(0x00FF00)
          .WithFields(fields)
          .Build()
      ]
    );
    var thread = await channel.CreateThreadAsync($"order {e.order.Data.Id}", message: message);
    if (thread == null) {
      MyLogger.Logger.LogError("Failed to create thread for order {orderId}", e.order.Data.Id);
      return;
    }

    orderThreads[e.order.Data.Id] = thread;
  }

  static async Task OnLiveBrokerOrderUpdateEvent(LiveBrokerOrderUpdateEvent e) {
    SocketTextChannel target = channel;
    var threadFound = orderThreads.TryGetValue(e.rootOrder.orderId, out var thread);
    if (threadFound) {
      target = thread!;
    }

    if (!threadFound) {
      MyLogger.Logger.LogError("Thread ID not found for order {orderId}", e.rootOrder.orderId);
    }

    var fields = new List<EmbedFieldBuilder> {
      new EmbedFieldBuilder()
        .WithName("order.status")
        .WithValue(e.order.status.ToString())
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.quantityFilled")
        .WithValue(e.order.quantityFilled)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.price")
        .WithValue(e.order.price)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.updateTime")
        .WithValue(e.order.updateTime)
        .WithIsInline(true),
    };

    if (e.orderResp == null) {
      await Retry(async () => await target.SendMessageAsync(
        text: "LiveBroker order update failed",
        embeds: [
          new EmbedBuilder()
            .WithTitle("LiveBroker order update failed")
            .WithDescription($"Order failed; orderId={e.rootOrder.orderId}")
            .WithColor(0xFF0000)
            .WithFields(fields)
            .Build()
        ]
      ));
      return;
    }

    if (e.orderResp.Error != null) {
      await Retry(async () => await target.SendMessageAsync(
        text: "LiveBroker order update error",
        embeds: [
          new EmbedBuilder()
            .WithTitle("LiveBroker order update error")
            .WithDescription($"Order error on orderId={e.rootOrder.orderId}\n{e.orderResp.Error.Message}")
            .WithColor(0xFFBB00)
            .WithFields(fields)
            .Build()
        ]
      ));
      return;
    }
    await Retry(async () => await target.SendMessageAsync(
      text: threadFound ? "LiveBroker order updated" : "Missing thread for LiveBroker order update; creating new thread",
      embeds: [
        new EmbedBuilder()
          .WithTitle($"LiveBroker order updated")
          .WithDescription($"")
          .WithColor(0xFFFF00)
          .WithFields(fields)
          .Build()
      ]
    ));
  }

  static async Task OnLiveBrokerOrderFilledEvent(LiveBrokerOrderFilledEvent e) {
    SocketTextChannel target = channel;
    var threadFound = orderThreads.TryGetValue(e.rootOrder.orderId, out var thread);
    if (threadFound) {
      target = thread!;
    }

    if (!threadFound) {
      MyLogger.Logger.LogError("Thread ID not found for order {orderId}", e.rootOrder.orderId);
    }

    var fields = new List<EmbedFieldBuilder> {
      new EmbedFieldBuilder()
        .WithName("order.status")
        .WithValue(e.rootOrder.status.ToString())
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.price")
        .WithValue(e.rootOrder.price)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.quantityFilled")
        .WithValue(e.rootOrder.quantityFilled)
        .WithIsInline(true),
      new EmbedFieldBuilder()
        .WithName("order.updateTime")
        .WithValue(e.rootOrder.updateTime)
        .WithIsInline(true),
    };

    await Retry(async () => await target.SendMessageAsync(
      text: threadFound ? "LiveBroker order filled" : "Missing thread for LiveBroker order filled; creating new thread",
      embeds: [
        new EmbedBuilder()
          .WithTitle($"LiveBroker order filled")
          .WithDescription($"")
          .WithColor(0x00FF00)
          .WithFields(fields)
          .Build()
      ]
    ));
  }

  static async Task OnLiveBrokerOrderDelayedEvent(LiveBrokerOrderDelayedEvent e) {
    var description = @$"Order delayed for {e.trader.Id} on {e.candle.symbolId}
trader: {e.trader.config.info.name} for {e.trader.config.dataset.symbolId}-{e.trader.config.dataset.interval.ToIntervalString()}
signal: {e.signal.kind}:{e.signal.confidence}
candle: {e.candle.ts}
delay: {(DateTime.Now - e.candle.ts).TotalHours} hours";

    await Retry(async () => await channel.SendMessageAsync(
      text: "LiveBroker order delayed",
      embeds: [
        new EmbedBuilder()
          .WithTitle("LiveBroker order delayed")
          .WithDescription(description)
          .WithColor(0xFF0000)
          .Build()
      ]
    ));
  }

  static async void OnException(HttpContext? context, Exception e) {
    try {
      var embed = new EmbedBuilder()
        .WithTitle(e.Message.Substring(0, 255))
        .WithDescription(e.ToString())
        .WithColor(0xFF0000)
        .Build();

      await channel.SendMessageAsync(
        text: "Unhandled Exception",
        embeds: [embed]
      );
    } catch {}
  }

  static async Task Retry(Func<Task> task, int maxRetries = 3, int delay = 1000) {
    for (int i = 0; i < maxRetries; i++) {
      try {
        await task();
        return;
      } catch (Exception e) {
        MyLogger.Logger.LogError(e, "Retrying task {i}/{maxRetries}", i + 1, maxRetries);
        await Task.Delay(delay);
      }
    }
    MyLogger.Logger.LogError("Max retries reached for task");
  }
}
