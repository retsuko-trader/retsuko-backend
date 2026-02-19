using Coravel;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Retsuko.Clients;
using Retsuko.Core;
using Retsuko.Plugins;

const string SERVICE_NAME = "retsuko-backend";
const string OTE_URL = "http://localhost:4317";

StrategyClient.Init();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddScheduler();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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
    .AddHttpClientInstrumentation()
    .AddRedisInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
      otlp.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
    }))
  .WithMetrics(metrics => metrics
    .AddMeter(MetricsMeter.Name)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddProcessInstrumentation()
    .AddRuntimeInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
      otlp.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
    }));

builder.Services.AddSingleton(MyTracer.Tracer);
builder.Services.AddControllers();
builder.Services.AddMvc()
  .AddJsonOptions(options => {
    // options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

var app = builder.Build();
app.UseExceptionHandler();

app.Services.UseScheduler(scheduler => {
  scheduler.ScheduleAsync(DiscordCron.Job)
    .EverySeconds(10)
    .PreventOverlapping("discord-cron");
});

MyLogger.Logger = app.Logger;

await Retsuko.Plugins.Discord.Initialize();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
