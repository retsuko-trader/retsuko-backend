public interface IBroker {
  public Task<Trade?> HandleAdvice(Candle candle, Signal signal);
}
