namespace Retsuko.Core;

public enum SignalKind {
  openLong,
  openShort,
  closeLong,
  closeShort,
}

public record Signal(
  SignalKind kind,
  double confidence
) {
  public static explicit operator Signal(SignalKind kind) {
    return new Signal(kind, 1);
  }

  public static Signal openShort =>  new(SignalKind.openShort, 1);
  public static Signal openLong => new(SignalKind.openLong, 1);
  public static Signal closeShort => new(SignalKind.closeShort, 1);
  public static Signal closeLong => new(SignalKind.closeLong, 1);
}
