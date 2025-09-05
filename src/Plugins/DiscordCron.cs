namespace Retsuko.Plugins;

public static class DiscordCron {
  public static async Task Job() {
    using var span = MyTracer.Tracer.StartRootSpan("DiscordCron.Job");
    var portfolio = await PortfolioService.Get();
    await Discord.SetPresence(portfolio);
  }
}
