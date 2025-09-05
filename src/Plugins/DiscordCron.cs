namespace Retsuko.Plugins;

public static class DiscordCron {
  public static async Task Job() {
    var portfolio = await PortfolioService.Get();
    await Discord.SetPresence(portfolio);
  }
}
