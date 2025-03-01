namespace Retsuko.Core;

public enum SignalKind {
  @long,
  @short,
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

  public static Signal @short =>  new(SignalKind.@short, 1);
  public static Signal @long => new(SignalKind.@long, 1);
  public static Signal closeShort => new(SignalKind.closeShort, 1);
  public static Signal closeLong => new(SignalKind.closeLong, 1);
}
