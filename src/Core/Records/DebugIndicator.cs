namespace Retsuko.Core;

public record DebugIndicatorEntry(
  long ts,
  float value
);

public record DebugIndicator(
  string name,
  int index,
  DebugIndicatorEntry[] values
);
