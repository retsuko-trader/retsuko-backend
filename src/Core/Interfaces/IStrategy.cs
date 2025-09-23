namespace Retsuko.Core;

public record struct StrategyUpdateResult(
  Candle candle,
  Signal? signal
);

public interface IStrategy {
  Task Init(string? state = null, bool debug = false);
  Task Preload(Candle candle);
  Task Update(Candle candle);
  Task<StrategyUpdateResult?> GetUpdateResult();
  Task FinishInputs();
  Task<string> GetFinalState();
  Task<DebugIndicator[]> GetDebugIndicators();
}
