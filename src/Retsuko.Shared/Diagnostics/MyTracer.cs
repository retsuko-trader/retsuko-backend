using OpenTelemetry.Trace;

namespace Retsuko.Diagnostics;

public static class MyTracer {
  public static string SERVICE_NAME;

  public static readonly Tracer Tracer = TracerProvider.Default.GetTracer(SERVICE_NAME);
}
