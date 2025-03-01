namespace Retsuko.Core;

public interface IBroker {
  public Task<Trade?> HandleAdvice(Candle candle, Signal signal);

  public Portfolio GetPortfolio();
  public double InitialBalance { get; }
}
