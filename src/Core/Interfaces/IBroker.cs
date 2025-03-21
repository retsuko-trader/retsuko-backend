namespace Retsuko.Core;

public interface IBroker: ISerializable {
  public Task<Trade?> HandleAdvice(Candle candle, Signal signal);

  public Portfolio GetPortfolio();
  public double InitialBalance { get; }
}
