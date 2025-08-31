using Grpc.Core;
using Retsuko.Clients;
using Google.Protobuf.WellKnownTypes;

namespace Retsuko.Core;

public class StrategyLazy: IStrategy, IDisposable {
  protected readonly AsyncDuplexStreamingCall<StrategyInput, StrategyLazyOutput> call;

  private readonly string name;
  private readonly string config;

  private StrategyOutputState resultState;

  protected StrategyLazy(
    AsyncDuplexStreamingCall<StrategyInput, StrategyLazyOutput> call,
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
    await call.RequestStream.WriteAsync(new StrategyInput {
      Create = new StrategyInputCreate {
        Name = name,
        Config = config,
        State = state ?? ""
      }
    });
  }

  public async Task Preload(Candle candle) {
    await call.RequestStream.WriteAsync(new StrategyInput {
      Preload = new StrategyInputPreload {
        Candle = new CandleRaw {
          Ts = Timestamp.FromDateTime(candle.ts.ToUniversalTime()),
          Open = candle.open,
          Close = candle.close,
          High = candle.high,
          Low = candle.low,
          Volume = candle.volume
        },
      },
    });
  }

  public virtual async Task Update(Candle candle) {
    await call.RequestStream.WriteAsync(new StrategyInput {
      Update = new StrategyInputUpdate {
        Candle = new CandleRaw {
          Ts = Timestamp.FromDateTime(candle.ts.ToUniversalTime()),
          Open = candle.open,
          Close = candle.close,
          High = candle.high,
          Low = candle.low,
          Volume = candle.volume
        },
      },
    });
  }

  public virtual async Task<StrategyUpdateResult?> GetUpdateResult() {
    if (!await call.ResponseStream.MoveNext()) {
      MyLogger.Logger.LogError("fatal; StrategyLazy {strategy} stream ended unexpectedly, config={config}", name, config);
      return null;
    }

    var response = call.ResponseStream.Current;
    if (response.State != null) {
      resultState = response.State;
      return null;
    }

    var signal = response.Signal;

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
    var call = StrategyClient.runnerClient.RunLazy();
    return new StrategyLazy(call, name, config);
  }
}
