using Grpc.Core;
using Retsuko.Clients;
using Google.Protobuf.WellKnownTypes;

namespace Retsuko.Core;

public class Strategy: IDisposable {
  private readonly AsyncDuplexStreamingCall<StrategyInput, StrategyOutput> call;

  private readonly string name;
  private readonly string config;

  protected Strategy(
    AsyncDuplexStreamingCall<StrategyInput, StrategyOutput> call,
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

  public async Task<Signal?> Update(Candle candle) {
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

    await call.ResponseStream.MoveNext();
    var response = call.ResponseStream.Current;
    var signal = response.Signal;
    if (signal == null) {
      return null;
    }

    return new Signal((SignalKind)signal.Kind, signal.Confidence);
  }

  public async Task<string> EndAndGetState() {
    await call.RequestStream.CompleteAsync();
    await call.ResponseStream.MoveNext();
    var response = call.ResponseStream.Current;
    return response.State.State;
  }

  public static Strategy Create(string name, string config) {
    var call = StrategyClient.runnerClient.Run();
    return new Strategy(call, name, config);
  }
}
