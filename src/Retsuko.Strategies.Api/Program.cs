using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Retsuko.Diagnostics;
using Retsuko.Strategies.GrpcHandlers;

var builder = WebApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();

string SERVICE_NAME = isDevelopment ? "retsuko-strategy-dev" : "retsuko-strategy";
MyTracer.SERVICE_NAME = SERVICE_NAME;
const string OTE_URL = "http://localhost:4317";

builder.Logging.AddOpenTelemetry(options => {
  options
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SERVICE_NAME))
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
      otlp.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
    });
});

builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource.AddService(SERVICE_NAME))
  .WithTracing(tracing => tracing
    .AddSource(SERVICE_NAME)
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SERVICE_NAME))
    .AddAspNetCoreInstrumentation()
    .AddGrpcClientInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
      otlp.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
    }))
  .WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
      otlp.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
    }));


builder.WebHost.ConfigureKestrel(
  options => {
    var tempPath = Path.GetTempPath();
    var path = builder.Environment.IsDevelopment() ? "retsuko-dev.sock" : "retsuko.sock";
    var sockPath = Path.Combine(tempPath, path);
    options.ListenUnixSocket(sockPath, listenOptions => {
      listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
  }
);

builder.Services.AddGrpc();

builder.Services.AddSingleton(MyTracer.Tracer);
builder.Services.AddControllers();

var app = builder.Build();

MyLogger.Logger = app.Logger;

app.MapControllers();

app.MapGrpcService<StrategyLoadService>();
app.MapGrpcService<StrategyRunService>();
app.MapGrpcService<StrategyCheckService>();

app.Run();
