using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Binance.Net.Enums;
using Microsoft.AspNetCore.Mvc;
using Retsuko.Diagnostics;

const string SERVICE_NAME = "retsuko-worker";
const string OTE_URL = "http://localhost:4317";

var builder = WebApplication.CreateBuilder(args);

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
    .AddAspNetCoreInstrumentation(asp => {
      asp.Filter = ctx => {
        return ctx.Request.Path != "/health";
      };
    })
    .AddHttpClientInstrumentation(http => {
      http.EnrichWithHttpRequestMessage = (activity, message) => {
        if (message.RequestUri?.AbsoluteUri == Const.CALLBACK_URL) {
          activity.DisplayName = $"{message.Method} {{retsuko-backend}}{message.RequestUri.AbsolutePath}";
        }
      };
    })
    .AddRedisInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }))
  .WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(otlp => {
      otlp.Endpoint = new Uri(OTE_URL);
      otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }));

builder.Services.AddSingleton(MyTracer.Tracer);

var app = builder.Build();
MyLogger.Logger = app.Logger;

var subscriber = new Subscriber();
await subscriber.Init();

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/subscribe", async (SubscriptionReq req) => {
  await subscriber.Subscribe(req.id, req.symbol, (KlineInterval)req.interval);
  MyLogger.Logger.LogInformation("Subscribed {req.id} to {req.symbol} with interval {req.interval}", req.id, req.symbol, req.interval);
  return Results.Ok();
});

app.MapPost("/subscribe/{id}/reload", async (string id, [FromBody]SubscriptionReloadReq req) => {
  await subscriber.Reload(id, req.count);
  MyLogger.Logger.LogInformation("Manually reloaded {id} for {count} klines", id, req.count);
  return Results.Ok();
});

app.MapDelete("/subscribe/{id}", async (string id) => {
  await subscriber.Unsubscribe(id);
  MyLogger.Logger.LogInformation("Unsubscribed {id}", id);
  return Results.Ok();
});

app.Run();

record SubscriptionReq(string id, string symbol, int interval);

record SubscriptionReloadReq(int count);
