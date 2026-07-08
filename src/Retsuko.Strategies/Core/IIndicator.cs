namespace Retsuko.Strategies.Core;

public interface IIndicator : ISerializable {
  public bool Ready { get; }
  public double Value { get; }
  public void Update(Candle candle);
}
