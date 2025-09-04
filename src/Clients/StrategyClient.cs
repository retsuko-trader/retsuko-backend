using System.Net;
using System.Net.Sockets;
using Grpc.Net.Client;

namespace Retsuko.Clients;

public class StrategyClient {
  public static GStrategyLoader.GStrategyLoaderClient loaderClient { get; private set; }
  public static GStrategyLoader.GStrategyLoaderClient devLoaderClient { get; private set; }
  public static GStrategyRunner.GStrategyRunnerClient runnerClient { get; private set; }
  public static GStrategyRunner.GStrategyRunnerClient devRunnerClient { get; private set; }

  public static void Init() {
    InitDevelopment();
    InitProduction();
  }

  private static void InitProduction() {
    var udsEndpoint = new UnixDomainSocketEndPoint(Path.Combine(Path.GetTempPath(), "retsuko.sock"));
    var factory = new SocketFactory(udsEndpoint);
    var socketHttpHandler = new SocketsHttpHandler {
      ConnectCallback = factory.ConnectAsync,
    };

    var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions {
      HttpHandler = socketHttpHandler
    });

    loaderClient = new GStrategyLoader.GStrategyLoaderClient(channel);
    runnerClient = new GStrategyRunner.GStrategyRunnerClient(channel);
  }

  private static void InitDevelopment() {
    var udsEndpoint = new UnixDomainSocketEndPoint(Path.Combine(Path.GetTempPath(), "retsuko-dev.sock"));
    var factory = new SocketFactory(udsEndpoint);
    var socketHttpHandler = new SocketsHttpHandler {
      ConnectCallback = factory.ConnectAsync,
    };

    var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions {
      HttpHandler = socketHttpHandler
    });

    devLoaderClient = new GStrategyLoader.GStrategyLoaderClient(channel);
    devRunnerClient = new GStrategyRunner.GStrategyRunnerClient(channel);

  }

  class SocketFactory(EndPoint endpoint) {
    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _, CancellationToken cancellationToken = default) {
      var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

      try {
        await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return new NetworkStream(socket, ownsSocket: true);
      } catch {
        socket.Dispose();
        throw;
      }
    }
  }
}
