using Discord;
using Discord.Webhook;
using Retsuko.Core.Events;

namespace Retsuko.Plugins;

public static class Discord {
  static string webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? "";
  static DiscordWebhookClient webhook = new(webhookUrl);

  public static void Initialize() {
    EventDispatcher.OnLiveBrokerEvent += OnLiveBrokerEvent;
    EventDispatcher.OnException += OnException;
  }

  static async void OnLiveBrokerEvent(LiveBrokerEvent e) {
    if (e is LiveBrokerGotSignalEvent signalEvent) {
      await OnLiveBrokerGotSignalEvent(signalEvent);
    }
  }

  static async Task OnLiveBrokerGotSignalEvent(LiveBrokerGotSignalEvent e) {

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
