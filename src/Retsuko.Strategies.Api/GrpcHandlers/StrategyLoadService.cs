using Grpc.Core;
using Retsuko.Strategies.Services;

namespace Retsuko.Strategies.GrpcHandlers;

public class StrategyLoadService : GStrategyLoader.GStrategyLoaderBase {
  public override Task<StrategyList> GetStrategies(EmptyReq request, ServerCallContext context) {
    var strategies = StrategyLoader.GetStrategyEntries()
      .Select(x => new GStrategy { Name = x.Name, Config = x.Config });

    var result = new StrategyList();
    result.Strategies.AddRange(strategies);

    return Task.FromResult(result);
  }
}
