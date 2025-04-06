using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Retsuko.Core;
using Retsuko.Migrations;

await Migrations.CreateLiveTrader();

const string SERVICE_NAME = "retsuko-backend";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

const string OTE_URL = "http://localhost:4317";

builder.Logging.AddOpenTelemetry(options => {
  options
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SERVICE_NAME))
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    });
});
builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource.AddService(SERVICE_NAME))
  .WithTracing(tracing => tracing
    .AddSource(SERVICE_NAME)
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SERVICE_NAME))
    .AddAspNetCoreInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }))
  .WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }));

builder.Services.AddSingleton(MyTracer.Tracer);
builder.Services.AddControllers();
builder.Services.AddMvc()
  .AddJsonOptions(options => {
    // options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

var app = builder.Build();
app.UseExceptionHandler();

MyLogger.Logger = app.Logger;

var strategies = string.Join(',', StrategyLoader.strategies.Select(x => x.Name));
MyLogger.Logger.LogInformation("Available strategies: {strategies}", strategies);

await Retsuko.Plugins.Discord.Initialize();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
