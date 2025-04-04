namespace Retsuko.Core;

public record DebugIndicator(
  long ts,
  float value
);

public record DebugIndicatorInput(
  string name,
  int index,
  float value
);

public record ExtDebugIndicator(
  string name,
  int index,
  IEnumerable<DebugIndicator> values
);
