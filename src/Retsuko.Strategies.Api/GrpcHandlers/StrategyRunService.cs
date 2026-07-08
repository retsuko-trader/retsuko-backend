using Grpc.Core;
using Retsuko.Diagnostics;
using Retsuko.Strategies.Core;
using Retsuko.Strategies.Services;

namespace Retsuko.Strategies.GrpcHandlers;

public class StrategyRunService : GStrategyRunner.GStrategyRunnerBase {
  public override async Task Run(IAsyncStreamReader<StrategyRunInput> requestStream, IServerStreamWriter<StrategyRunOutput> responseStream, ServerCallContext context) {
    var tracer = MyTracer.Tracer;
    using var createSpan = tracer.StartActiveSpan("StrategyRunService.Run.Create");

    await requestStream.MoveNext();
    var create = requestStream.Current.Create;
    if (create == null) {
      throw new RpcException(new Status(StatusCode.InvalidArgument, "First message must be Create"));
    }

    var strategy = StrategyLoader.CreateStrategy(create.Name, create.Config);
    if (strategy == null) {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Strategy {create.Name} not found"));
    }

    if (!string.IsNullOrEmpty(create.State)) {
      strategy.Deserialize(create.State);
    }

    createSpan.End();

    using var processSpan = tracer.StartActiveSpan("StrategyRunService.Run.Process");
    while (await requestStream.MoveNext(context.CancellationToken) && !context.CancellationToken.IsCancellationRequested) {
      var input = requestStream.Current;
      if (input.Preload != null) {
        await strategy.Preload(input.Preload.Candle);
      } else if (input.Update != null) {
        var signal = await strategy.Update(input.Update.Candle);
        await responseStream.WriteAsync(new StrategyRunOutput {
          Signal = signal == null ? null : new StrategyRunOutputSignal {
            Kind = (int)signal.kind,
            Confidence = signal.confidence
          },
        }, context.CancellationToken);
      } else {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown message type"));
      }
    }

    processSpan.End();

    using var responseSpan = tracer.StartActiveSpan("StrategyRunService.Run.Response");
    await responseStream.WriteAsync(new StrategyRunOutput {
      State = new StrategyRunOutputState {
        State = strategy.Serialize(),
      },
    }, context.CancellationToken);
    responseSpan.End();
  }

  public override async Task RunLazy(IAsyncStreamReader<StrategyRunInputBatch> requestStream, IServerStreamWriter<StrategyRunLazyOutputBatch> responseStream, ServerCallContext context) {
    var tracer = MyTracer.Tracer;
    using var createSpan = tracer.StartActiveSpan("StrategyRunService.RunLazy.Create");

    await requestStream.MoveNext();
    var create = requestStream.Current.Create;
    if (create == null) {
      throw new RpcException(new Status(StatusCode.InvalidArgument, "First message must be Create"));
    }

    var strategy = StrategyLoader.CreateStrategy(create.Name, create.Config);
    if (strategy == null) {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Strategy {create.Name} not found"));
    }

    if (!string.IsNullOrEmpty(create.State)) {
      strategy.Deserialize(create.State);
    }

    createSpan.End();

    using var processSpan = tracer.StartActiveSpan("StrategyRunService.RunLazy.Process");
    var signalResponses = new Queue<StrategyRunOutputSignalWithCandle>();
    var debugIndicators = new DebugIndicatorHandler();

    while (await requestStream.MoveNext(context.CancellationToken) && !context.CancellationToken.IsCancellationRequested) {
      var input = requestStream.Current;
      if (input.Preload != null) {
        foreach (var candle in input.Preload.Candles) {
          await strategy.Preload(candle);
        }
      } else if (input.Update != null) {
        foreach (var candle in input.Update.Candles) {
          var signal = await strategy.Update(candle);
          if (signal != null) {
            signalResponses.Enqueue(new StrategyRunOutputSignalWithCandle {
              Candle = candle,
              Kind = (int)signal.kind,
              Confidence = signal.confidence
            });
          }

          if (create.Debug) {
            var debug = await strategy.Debug(candle);
            foreach (var item in debug) {
              debugIndicators.Add(candle, item);
            }
          }
        }

      } else {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown message type"));
      }
    }

    processSpan.End();

    using var signalResponseSpan = tracer.StartActiveSpan("StrategyRunService.RunLazy.SignalResponse");
    var chunks = signalResponses.Chunk(200);
    foreach (var chunk in chunks) {
      if (context.CancellationToken.IsCancellationRequested) {
        break;
      }

      var output = new StrategyRunLazyOutputBatch();
      output.Outputs.AddRange(chunk.Select(response => new StrategyRunLazyOutput { Signal = response }));
      await responseStream.WriteAsync(output, context.CancellationToken);
    }
    signalResponseSpan.End();

    using var responseSpan = tracer.StartActiveSpan("StrategyRunService.RunLazy.Response");

    var responseOutput = new StrategyRunLazyOutputBatch();
    responseOutput.Outputs.Add(new StrategyRunLazyOutput {
      State = new StrategyRunOutputState {
        State = strategy.Serialize(),
      },
    });
    await responseStream.WriteAsync(responseOutput, context.CancellationToken);

    var debugChunks = debugIndicators.GetResult().Chunk(2);
    foreach (var debugChunk in debugChunks) {
      if (context.CancellationToken.IsCancellationRequested) {
        break;
      }

      var output = new StrategyRunLazyOutputBatch();
      var item = new StrategyRunOutputDebug();
      output.Outputs.AddRange(debugChunk.Select(item => new StrategyRunLazyOutput {
        Debug = new StrategyRunOutputDebug { Indicator = item },
      }));
      await responseStream.WriteAsync(output, context.CancellationToken);
    }

    responseSpan.End();
  }

  class DebugIndicatorHandler {
    private Dictionary<string, GDebugIndicator> data = new();

    public void Add(GCandle candle, DebugIndicatorInput input) {
      if (!data.TryGetValue(input.name, out var item)) {
        item = new GDebugIndicator {
          Name = input.name,
          Index = input.index,
          Values = { }
        };
        data[input.name] = item;
      }

      item.Values.Add(new GDebugIndicatorEntry {
        Ts = candle.Ts.Seconds * 1000,
        Value = input.value
      });
    }

    public IEnumerable<GDebugIndicator> GetResult() {
      return data.Values;
    }
  }
}
