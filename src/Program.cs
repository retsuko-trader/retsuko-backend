using System.Text.Json.Serialization;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string SERVICE_NAME = "retsuko-backend";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(options => {
  options
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SERVICE_NAME))
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri("http://localhost:4317");
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    });
});
builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource.AddService(SERVICE_NAME))
  .WithTracing(tracing => tracing
    .AddSource("Retsuko")
    .AddAspNetCoreInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri("http://localhost:4317");
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }))
  .WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri("http://localhost:4317");
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }));

builder.Services.AddSingleton(MyTracer.Tracer);
builder.Services.AddControllers();
builder.Services.AddMvc()
  .AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

var app = builder.Build();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
