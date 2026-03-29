using Grpc.Core;
using Retsuko.Clients;
using Google.Protobuf.WellKnownTypes;

namespace Retsuko.Core;

public class Strategy: IStrategy, IDisposable {
  protected readonly AsyncDuplexStreamingCall<StrategyRunInput, StrategyRunOutput> call;

  private readonly string name;
  private readonly string config;

  private Candle lastCandle;

  protected Strategy(
    AsyncDuplexStreamingCall<StrategyRunInput, StrategyRunOutput> call,
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

  public async Task Init(string? state = null, bool debug = false) {
    await call.RequestStream.WriteAsync(new StrategyRunInput {
      Create = new StrategyRunInputCreate {
        Name = name,
        Config = config,
        State = state ?? "",
        Debug = debug
      }
    });
  }

  public async Task Preload(Candle candle) {
    await call.RequestStream.WriteAsync(new StrategyRunInput {
      Preload = new StrategyRunInputPreload {
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
    await call.RequestStream.WriteAsync(new StrategyRunInput {
      Update = new StrategyRunInputUpdate {
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

    lastCandle = candle;
  }

  public virtual async Task<StrategyUpdateResult?> GetUpdateResult() {
    if (!await call.ResponseStream.MoveNext()) {
      MyLogger.Logger.LogError("fatal; Strategy {strategy} stream ended unexpectedly, config={config}", name, config);
      return null;
    }

    var response = call.ResponseStream.Current;
    var signal = response.Signal;
    if (signal == null) {
      return new(lastCandle, null);
    }

    return new(lastCandle, new Signal((SignalKind)signal.Kind, signal.Confidence));
  }

  public async Task FinishInputs() {
    await call.RequestStream.CompleteAsync();
  }

  public async Task<string> GetFinalState() {
    await call.ResponseStream.MoveNext();
    var response = call.ResponseStream.Current;
    return response.State.State;
  }

  public async Task<DebugIndicator[]> GetDebugIndicators() {
    await ValueTask.CompletedTask;
    return [];
  }

  public static Strategy Create(string name, string config) {
    var call = StrategyClient.runnerClient.Run();
    return new Strategy(call, name, config);
  }
}
