using Retsuko.Core;

namespace Retsuko.Plugins;

public static class DiscordCron {
  public static async Task Job() {
    using var span = MyTracer.Tracer.StartActiveSpan("DiscordCron.Job");

    var portfolio = await PortfolioService.Get();
    await Discord.SetPresence(portfolio);

    await MetricsMeter.Update(portfolio);
  }
}
