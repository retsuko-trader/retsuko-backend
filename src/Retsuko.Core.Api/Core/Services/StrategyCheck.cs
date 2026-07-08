using Retsuko.Clients;

namespace Retsuko.Core;

public class StrategyCheck(string name, string config, string state) {
  public async Task<StrategyCheckOutputConsistency> Check() {
    var client = StrategyClient.devCheckClient;
    var result = await client.CheckConsistencyAsync(new StrategyCheckInputLoad {
      Name = name,
      Config = config,
      State = state
    });

    return result;
  }

  public async Task<string> Dump() {
    var client = StrategyClient.devCheckClient;
    var result = await client.DumpAsync(new StrategyCheckInputLoad {
      Name = name,
      Config = config,
      State = state
    });

    return result.State;
  }
}
