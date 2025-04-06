using Discord;
using Discord.Webhook;
using Retsuko.Core.Events;

namespace Retsuko.Plugins;

public static class Discord {
  static string webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? "";
  static DiscordWebhookClient webhook = new(webhookUrl);

  static Dictionary<long, ulong> orderThreadIds = new();

  public static void Initialize() {
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
      await webhook.SendMessageAsync(
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
      await webhook.SendMessageAsync(
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

    var threadId = await webhook.SendMessageAsync(
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
    orderThreadIds[e.order.Data.Id] = threadId;
  }

  static async Task OnLiveBrokerOrderUpdateEvent(LiveBrokerOrderUpdateEvent e) {
    var threadId = (ulong?)null;
    var threadFound = orderThreadIds.TryGetValue(e.rootOrder.orderId, out var threadId0);
    if (threadFound) {
      threadId = threadId0;
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
      await webhook.SendMessageAsync(
        text: "LiveBroker order update failed",
        threadId: threadId,
        embeds: [
          new EmbedBuilder()
            .WithTitle("LiveBroker order update failed")
            .WithDescription($"Order failed; orderId={e.rootOrder.orderId}")
            .WithColor(0xFF0000)
            .WithFields(fields)
            .Build()
        ]
      );
      return;
    }

    if (e.orderResp.Error != null) {
      await webhook.SendMessageAsync(
        text: "LiveBroker order update error",
        threadId: threadId,
        embeds: [
          new EmbedBuilder()
            .WithTitle("LiveBroker order update error")
            .WithDescription($"Order error on orderId={e.rootOrder.orderId}\n{e.orderResp.Error.Message}")
            .WithColor(0xFFBB00)
            .WithFields(fields)
            .Build()
        ]
      );
      return;
    }
    await webhook.SendMessageAsync(
      text: threadFound ? "LiveBroker order updated" : "Missing thread for LiveBroker order update; creating new thread",
      threadId: threadId,
      embeds: [
        new EmbedBuilder()
          .WithTitle($"LiveBroker order updated")
          .WithDescription($"")
          .WithColor(0xFFFF00)
          .WithFields(fields)
          .Build()
      ]
    );
  }

  static async Task OnLiveBrokerOrderFilledEvent(LiveBrokerOrderFilledEvent e) {
    var threadId = (ulong?)null;
    var threadFound = orderThreadIds.TryGetValue(e.rootOrder.orderId, out var threadId0);
    if (threadFound) {
      threadId = threadId0;
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

    await webhook.SendMessageAsync(
      text: threadFound ? "LiveBroker order filled" : "Missing thread for LiveBroker order filled; creating new thread",
      threadId: threadId,
      embeds: [
        new EmbedBuilder()
          .WithTitle($"LiveBroker order filled")
          .WithDescription($"")
          .WithColor(0x00FF00)
          .WithFields(fields)
          .Build()
      ]
    );
  }

  static async void OnException(HttpContext? context, Exception e) {
    var embed = new EmbedBuilder()
      .WithTitle(e.Message)
      .WithDescription(e.ToString())
      .WithColor(0xFF0000)
      .Build();

    await webhook.SendMessageAsync(
      text: "Unhandled Exception",
      embeds: [embed]
    );
  }
}
