using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Retsuko.Strategies.Services;

namespace Retsuko.Strategies.GrpcHandlers;

public class StrategyCheckService : GStrategyCheck.GStrategyCheckBase {
  public override Task<StrategyCheckOutputConsistency> CheckConsistency(StrategyCheckInputLoad request, ServerCallContext context) {
    var strategy = StrategyLoader.CreateStrategy(request.Name, request.Config);
    if (strategy == null) {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Strategy {request.Name} not found"));
    }

    strategy.Deserialize(request.State);

    var result = strategy.CheckConsistency();

    return Task.FromResult(new StrategyCheckOutputConsistency {
      Success = result.IsSuccess,
      Errors = { result.Errors },
    });
  }
  public override Task<StrategyCheckOutputDump> Dump(StrategyCheckInputLoad request, ServerCallContext context) {
    var strategy = StrategyLoader.CreateStrategy(request.Name, request.Config);
    if (strategy == null) {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Strategy {request.Name} not found"));
    }

    strategy.Deserialize(request.State);

    var result = strategy.Dump();

    return Task.FromResult(new StrategyCheckOutputDump {
      State = JsonSerializer.Serialize(result, jsonOptions),
    });
  }

  private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
    Converters = {
      new ProtobufTimestampConverter(),
    },
  };
}

class ProtobufTimestampConverter : JsonConverter<Timestamp> {
  public override Timestamp? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) {
    var dateTime = DateTime.Parse(reader.GetString()!);
    return Timestamp.FromDateTime(dateTime.ToUniversalTime());
  }

  public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options) {
    var dateTime = value.ToDateTime().ToUniversalTime();
    writer.WriteStringValue(dateTime.ToString("o"));
  }
}
