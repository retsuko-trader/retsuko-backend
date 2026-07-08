namespace Retsuko.Strategies.Core;

public record DebugIndicatorInput(
  string name,
  int index,
  float value
);

public record DebugIndicatorEntry(
  long ts,
  float value
);

public record DebugIndicator(
  string name,
  int index,
  List<DebugIndicatorEntry> values
);
