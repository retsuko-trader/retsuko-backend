using Grpc.Core;
using Retsuko.Clients;
using Google.Protobuf.WellKnownTypes;

namespace Retsuko.Core;

public class StrategyLazy: IStrategy, IDisposable {
  protected readonly AsyncDuplexStreamingCall<StrategyInputBatch, StrategyLazyOutputBatch> call;

  private readonly string name;
  private readonly string config;

  private StrategyOutputState resultState;

  private Queue<StrategyLazyOutput> outputs = new();

  protected StrategyLazy(
    AsyncDuplexStreamingCall<StrategyInputBatch, StrategyLazyOutputBatch> call,
    string name,
    string config
  ) {
    this.call = call;

    this.name = name;
    this.config = config;
  }

  public void Dispose() {
    call?.Dispose();
  }

  public async Task Init(string? state = null) {
    await call.RequestStream.WriteAsync(new StrategyInputBatch {
      Create = new StrategyInputCreate {
        Name = name,
        Config = config,
        State = state ?? ""
      }
    });
  }

  public async Task Preload(Candle candle) {
    await PreloadBulk([candle]);
  }

  public async Task PreloadBulk(IEnumerable<Candle> candles) {
    var batch = new StrategyInputBatch {
      Preload = new StrategyInputPreloadBatch {
        Candles = { candles.Select(c => new CandleRaw {
          Ts = Timestamp.FromDateTime(c.ts.ToUniversalTime()),
          Open = c.open,
          Close = c.close,
          High = c.high,
          Low = c.low,
          Volume = c.volume
        }) }
      }
    };

    await call.RequestStream.WriteAsync(batch);
  }

  public async Task Update(Candle candle) {
    await UpdateBulk([candle]);
  }

  public async Task UpdateBulk(IEnumerable<Candle> candles) {
    var batch = new StrategyInputBatch {
      Update = new StrategyInputUpdateBatch {
        Candles = { candles.Select(c => new CandleRaw {
          Ts = Timestamp.FromDateTime(c.ts.ToUniversalTime()),
          Open = c.open,
          Close = c.close,
          High = c.high,
          Low = c.low,
          Volume = c.volume
        }) }
      }
    };

    await call.RequestStream.WriteAsync(batch);
  }

  public virtual async Task<StrategyUpdateResult?> GetUpdateResult() {
    if (outputs.Count == 0) {
      if (!await call.ResponseStream.MoveNext()) {
        MyLogger.Logger.LogError("fatal; StrategyLazy {strategy} stream ended unexpectedly, config={config}", name, config);
        return null;
      }

      var response = call.ResponseStream.Current;
      foreach (var output in response.Outputs) {
        outputs.Enqueue(output);
      }
    }

    var item = outputs.Dequeue();

    if (item.State != null) {
      resultState = item.State;
      return null;
    }

    var signal = item.Signal;

    return new(
      Candle.From(signal.Candle),
      new Signal((SignalKind)signal.Kind, signal.Confidence)
    );
  }

  public async Task FinishInputs() {
    await call.RequestStream.CompleteAsync();
  }

  public async Task<string> GetFinalState() {
    await ValueTask.CompletedTask;
    return resultState.State;
  }

  public static StrategyLazy Create(string name, string config) {
    var call = StrategyClient.devRunnerClient.RunLazy();
    return new StrategyLazy(call, name, config);
  }
}
