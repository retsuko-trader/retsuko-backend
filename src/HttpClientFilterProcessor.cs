using System.Diagnostics;
using OpenTelemetry;

public class HttpClientFilterProcessor : BaseProcessor<Activity> {
  private static readonly string[] FilteredUrls = new[] {
    "https://fapi.binance.com/fapi/v3/positionRisk?*",
    "https://fapi.binance.com/fapi/v3/account?*",
    "https://fapi.binance.com/fapi/v2/ticker/price?*",
  };

  public override void OnEnd(Activity activity) {
    if (activity.DisplayName == "DiscordCron.Job") {
      activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
      activity.IsAllDataRequested = false;
    }
  }
}
