namespace Retsuko.Core;

public record DebugIndicator(
  int ts,
  int index,
  float value
);

public record DebugIndicatorInput(
  string name,
  int index,
  float value
);
